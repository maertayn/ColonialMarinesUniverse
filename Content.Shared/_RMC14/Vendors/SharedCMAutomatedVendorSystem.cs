ApplyVendUserChanges so requisition orders grant too.
        // if (entity.TryComp(out CMChangeUserOnVendComponent? change, _compFactory) &&
        //     change.AddComponents != null)
        // {
        //     EntityManager.AddComponents(actor, change.AddComponents);
        // }
        ApplyVendUserChanges(entry.Id, actor);
    }

    // cmu edit start: orders filled outside the vendor's own menu, like requisition slips
    /// <summary>
    ///     Hands out one of this entry straight from the vendor's stock, taking it off the count the same way the menu
    ///     does. Ignores points, choices and job limits: the order was approved elsewhere. Server only.
    ///     With <paramref name="dropAt"/> the item is set down there instead of handed over.
    /// </summary>
    public bool TryVendFromStock(Entity<CMAutomatedVendorComponent> vendor, EntityUid actor, int sectionIndex, int entryIndex,
        Robust.Shared.Map.EntityCoordinates? dropAt = null)
    {
        if (_net.IsClient ||
            sectionIndex < 0 || sectionIndex >= vendor.Comp.Sections.Count ||
            entryIndex < 0 || entryIndex >= vendor.Comp.Sections[sectionIndex].Entries.Count)
        {
            return false;
        }

        var entry = vendor.Comp.Sections[sectionIndex].Entries[entryIndex];
        if (entry.Amount is <= 0 || !_prototypes.TryIndex(entry.Id, out var entity))
            return false;

        if (entry.Amount != null)
        {
            var stock = entry;
            if (entry.Box is { } box)
            {
                foreach (var stockSection in vendor.Comp.Sections)
                {
                    foreach (var stockEntry in stockSection.Entries)
                    {
                        if (stockEntry.Id == box)
                            stock = stockEntry;
                    }
                }
            }

            // entries are records, so compare references, not values
            stock.Amount -= ReferenceEquals(stock, entry) ? 1 : GetBoxRemoveAmount(entry);

            Dirty(vendor);
            AmountUpdated(vendor, stock);
        }

        var min = vendor.Comp.MinOffset;
        var max = vendor.Comp.MaxOffset;
        for (var i = 0; i < entry.Spawn; i++)
        {
            var offset = _random.NextVector2Box(min.X, min.Y, max.X, max.Y);
            var toVend = entity.TryComp(out CMVendorBundleComponent? bundle, _compFactory)
                ? bundle.Bundle
                // a list collection expression compiles to CollectionsMarshal.SetCount, which the client sandbox rejects
                : new List<EntProtoId> { entry.Id };
            foreach (var item in toVend)
            {
                if (dropAt is not { } drop)
                {
                    Vend(vendor, actor, item, offset, entry.ReplaceSlot);
                    continue;
                }

                // A little scatter so a full order doesn't land in one stack.
                var spawned = Spawn(item, drop.Offset(_random.NextVector2Box(-0.3f, -0.3f, 0.3f, 0.3f)));
                var vendedEv = new Content.Shared.CMU14.Vendors.CMUVendorVendedItemEvent(actor, spawned);
                RaiseLocalEvent(vendor, ref vendedEv);
            }
        }

        // CMU14: orders bypass the menu, so carry the menu's user-component grants over too.
        ApplyVendUserChanges(entry.Id, actor);

        return true;
    }
    // cmu edit end

    // CMU14 method
    private void ApplyVendUserChanges(EntProtoId toVend, EntityUid actor)
    {
        if (_prototypes.Index(toVend).TryComp(out CMChangeUserOnVendComponent? change, _compFactory)
                && change.AddComponents != null)
            EntityManager.AddComponents(actor, change.AddComponents);
    }

    private void Vend(EntityUid vendor, EntityUid player, EntProtoId toVend, Vector2 offset, SlotFlags? replaceSlot = null)
    {
        if (_prototypes.Index(toVend).TryComp(out CMVendorMapToSquadComponent? mapTo, _compFactory))
        {
            if (TryComp(player, out SquadMemberComponent? member) &&
                member.Squad is { } squad &&
                CompOrNull<MetaDataComponent>(squad)?.EntityPrototype is { } squadPrototype &&
                mapTo.Map.TryGetValue(squadPrototype.ID, out var mapped))
            {
                toVend = mapped;
            }
            else if (mapTo.Default is { } defaultVend)
            {
                toVend = defaultVend;
            }
            else
            {
                return;
            }
        }

        if (TryComp(vendor, out RMCRequisitionsVendorComponent? vendorComponent) && vendorComponent.Enabled &&
            _rmcMap.HasAnchoredEntityEnumerator<RMCRequisitionsChairComponent>(player.ToCoordinates(),
                out var requisitionsChair))
        {
            var itemPlacementOffset = requisitionsChair.Comp.OffsetItem;
            var finalPlacementCoordinates = requisitionsChair.Owner.ToCoordinates().Offset(itemPlacementOffset);
            var spawn = SpawnAtPosition(toVend, finalPlacementCoordinates);

            AfterVend(spawn, player, vendor, offset, true, replaceSlot);
        }
        else
        {
            // CMU14: yautja racks are solid multi-tile fixtures, so gear that is not
            // auto-equipped must land at the hunter's feet instead of on the rack
            var spawn = SpawnAtPosition(toVend, HasComp<YautjaGearRackComponent>(vendor)
                ? player.ToCoordinates()
                : vendor.ToCoordinates());
            AfterVend(spawn, player, vendor, offset, replaceSlot: replaceSlot);
        }
    }

    private void AfterVend(EntityUid spawn, EntityUid player, EntityUid vendor, Vector2 offset, bool vended = false, SlotFlags? replaceSlot = null)
    {
        EnsureComp<RMCRecentlyVendedComponent>(spawn);

        var mount = EnsureComp<WallMountComponent>(spawn);
        mount.Arc = Angle.FromDegrees(360);
        Dirty(spawn, mount);

        // cmu edit start: some vendors drop what they vend on top of themselves, with a sound, like a tray
        if (TryComp(vendor, out Content.Shared.CMU14.Vendors.CMUVendOntoMachineComponent? ontoMachine))
        {
            vended = true;
            if (ontoMachine.Sound != null)
                _audio.PlayPredicted(ontoMachine.Sound, vendor, player);
        }
        // cmu edit end

        if (!vended)
        {
            var grabbed = Grab(player, spawn, replaceSlot);
            if (!grabbed)
            {
                // A vendor can itself be inside a container, so detach failed output before placing it nearby.
                _container.TryRemoveFromContainer(spawn, force: true);

                // Yautja racks are solid fixtures rather than ordinary vending-machine walls. A failed auto-equip
                // must become a normal portable item and be placed outside the rack's collision footprint.
                if (HasComp<YautjaGearRackComponent>(vendor))
                {
                    RemCompDeferred<WallMountComponent>(spawn);

                    if (offset.LengthSquared() < 0.36f)
                    {
                        // Use a cardinal direction so both possible axes clear the rack's 0.45 half-size
                        // (a diagonal 0.6 vector would only move 0.42 on each axis).
                        if (MathF.Abs(offset.X) >= MathF.Abs(offset.Y) && MathF.Abs(offset.X) > 0.0001f)
                            offset = new Vector2(MathF.CopySign(0.6f, offset.X), 0);
                        else if (MathF.Abs(offset.Y) > 0.0001f)
                            offset = new Vector2(0, MathF.CopySign(0.6f, offset.Y));
                        else
                            offset = Vector2.UnitY * 0.6f;
                    }
                }

                if (TryComp(spawn, out TransformComponent? xform))
                    _transform.SetLocalPosition(spawn, xform.LocalPosition + offset, xform);
            }
        }

        var ev = new RMCAutomatedVendedUserEvent(spawn);
        RaiseLocalEvent(player, ref ev);

        // cmu edit start: let the vendor itself react to what it handed out
        var vendorEv = new Content.Shared.CMU14.Vendors.CMUVendorVendedItemEvent(player, spawn);
        RaiseLocalEvent(vendor, ref vendorEv);
        // cmu edit end

        _adminLog.Add(LogType.RMCVend,
            $"{ToPrettyString(player)} vended {ToPrettyString(spawn)} from vendor {ToPrettyString(vendor)}");
    }

    private bool Grab(EntityUid player, EntityUid item, SlotFlags? replaceSlot = null)
    {
        if (!HasComp<ItemComponent>(item))
            return false;

        if (TryAttachWebbing(player, item))
            return true;

        if (!TryComp(item, out ClothingComponent? clothing))
            return _hands.TryPickupAnyHand(player, item);

        if (replaceSlot != null)
        {
            EntityUid? itemToReplace = null;

            var slots = _inventory.GetSlotEnumerator(player, replaceSlot.Value);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                {
                    itemToReplace = slot.ContainedEntity;
                    _inventory.TryUnequip(player, slot.ID, true);
                    break;
                }
            }

            if (itemToReplace != null)
            {
                if (HasComp<StorageComponent>(item) && HasComp<StorageComponent>(itemToReplace))
                    _storage.TransferEntities(itemToReplace.Value, item);
            }
        }

        if (_cmInventory.TryEquipClothing(player, (item, clothing), doRangeCheck: false))
            return true;

        return _hands.TryPickupAnyHand(player, item);
    }

    private bool TryAttachWebbing(EntityUid player, EntityUid item)
    {
        if (HasComp<WebbingComponent>(item) &&
            _inventory.TryGetContainerSlotEnumerator(player, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (slot.ContainedEntity is { } contained &&
                    TryComp(contained, out WebbingClothingComponent? clothing) &&
                    _webbing.Attach((contained, clothing), item, player, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void SetPoints(Entity<CMVendorUserComponent> user, int points)
    {
        user.Comp.Points = points;
        Dirty(user);
    }

    public void SetChoiceWhitelist(Entity<CMVendorUserComponent> user, HashSet<string>? choices)
    {
        user.Comp.ChoiceWhitelist = choices;
        Dirty(user);
    }

    public void InitializeChoices(Entity<CMVendorUserComponent> user, IReadOnlyDictionary<string, int> choices)
    {
        foreach (var (id, value) in choices)
        {
            if (!user.Comp.Choices.ContainsKey(id))
                user.Comp.Choices[id] = value;
        }

        Dirty(user);
    }

    public void SetSections(Entity<CMAutomatedVendorComponent> vendor, List<CMVendorSection> sections)
    {
        vendor.Comp.Sections = sections;
        Dirty(vendor);
    }

    public void SetExtraPoints(Entity<CMVendorUserComponent> user, string key, int points)
    {
        user.Comp.ExtraPoints ??= new Dictionary<string, int>();
        user.Comp.ExtraPoints[key] = points;
        Dirty(user);
    }

    public bool TryTakeStockedItem(EntityCoordinates coords, float range, EntProtoId prototype, out EntityUid item)
    {
        item = default;

        _vendors.Clear();
        _entityLookup.GetEntitiesInRange(coords, range, _vendors);
        foreach (var vendor in _vendors)
        {
            foreach (var section in vendor.Comp.Sections)
            {
                foreach (var entry in section.Entries)
                {
                    if (entry.Id != prototype || entry.Amount is not { } amount || amount <= 0)
                        continue;

                    entry.Amount = amount - 1;
                    AmountUpdated(vendor, entry);
                    Dirty(vendor);

                    item = Spawn(prototype, coords);
                    return true;
                }
            }
        }

        return false;
    }

    public int GetStockedAmount(EntityCoordinates coords, float range, EntProtoId prototype)
    {
        var total = 0;
        _vendors.Clear();
        _entityLookup.GetEntitiesInRange(coords, range, _vendors);
        foreach (var vendor in _vendors)
        {
            foreach (var section in vendor.Comp.Sections)
            {
                foreach (var entry in section.Entries)
                {
                    if (entry.Id == prototype && entry.Amount is { } amount)
                        total += amount;
                }
            }
        }

        return total;
    }

    public void AmountUpdated(Entity<CMAutomatedVendorComponent> vendor, CMVendorEntry entry)
    {
        foreach (var section in vendor.Comp.Sections)
        {
            if (!section.HasBoxes)
                continue;

            foreach (var sectionEntry in section.Entries)
            {
                if (sectionEntry.Box is not { } box)
                    continue;

                if (entry.Id != box)
                    continue;

                sectionEntry.Amount = entry.Amount / GetBoxRemoveAmount(sectionEntry);
            }
        }
    }

    private int GetBoxRemoveAmount(CMVendorEntry entry)
    {
        if (entry.BoxSlots is not { } boxSlots)
        {
            if (!_prototypes.TryIndex(entry.Id, out var boxProto) ||
                !boxProto.TryComp(out CMItemSlotsComponent? slots, _compFactory) ||
                slots.Count is not { } count)
            {
                return 1;
            }

            boxSlots = count;
        }

        var amount = boxSlots;
        if (entry.BoxAmount is { } boxAmount)
            amount = boxAmount;

        return Math.Max(1, amount);
    }

    private void OnSpecEnteredCryostorageEvent(Entity<RMCSpecCryoRefundComponent> ent, ref EnteredCryostorageEvent args)
    {
        if (!TryComp<RMCVendorSpecialistComponent>(ent.Comp.Vendor, out var vendor))
            return;

        if (!vendor.GlobalSharedVends.TryGetValue(ent.Comp.Entry, out var current))
            return;

        if (current < 1) // How?
            return;

        vendor.GlobalSharedVends[ent.Comp.Entry] = current - 1;
        Dirty(ent.Comp.Vendor, vendor);
    }

    /// <summary>
    ///     Pushes <paramref name="newPoints"/> into the <see cref="CMAutomatedVendorComponent.CachedFactionWinPoints"/>
    ///     of every objective-point vendor that matches <paramref name="faction"/>, then dirties each one so the
    ///     client BUI receives the updated value regardless of whether the ObjectiveMasterComponent entity is in PVS.
    /// </summary>
    public void UpdateVendorFactionPointsCache(string faction, int newPoints)
    {
        var factionKey = faction.ToLowerInvariant();
        var q = EntityQueryEnumerator<CMAutomatedVendorComponent>();
        while (q.MoveNext(out var uid, out var comp))
        {
            if (!comp.UseObjectivePoints)
                continue;
            if (comp.Faction.ToLowerInvariant() != factionKey)
                continue;
            comp.CachedFactionWinPoints = newPoints;
            Dirty(uid, comp);
        }
    }
}
