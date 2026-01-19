// <copyright file="ItemList097.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Version097d.Items;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using Version095dItems = MUnique.OpenMU.Persistence.Initialization.Version095d.Items;

internal static class ItemList097
{
    private const string ItemListRelativePath = "Version097d/Items/ItemList097.dat";
    private const byte NoSlot = byte.MaxValue;

    public static HashSet<(byte Group, short Number)> LoadAllowedItems()
    {
        var itemListPath = Path.Combine(
            AppContext.BaseDirectory,
            ItemListRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(itemListPath))
        {
            return [];
        }

        var allowedItems = new HashSet<(byte Group, short Number)>();
        byte? currentGroup = null;

        foreach (var rawLine in File.ReadLines(itemListPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsGroupHeader(line, out var group))
            {
                currentGroup = group;
                continue;
            }

            if (currentGroup is null)
            {
                continue;
            }

            var firstToken = GetFirstToken(line);
            if (short.TryParse(firstToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                allowedItems.Add((currentGroup.Value, number));
            }
        }

        return allowedItems;
    }

    public static IReadOnlyList<ItemListEntry> LoadItems()
    {
        var itemListPath = Path.Combine(
            AppContext.BaseDirectory,
            ItemListRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(itemListPath))
        {
            return Array.Empty<ItemListEntry>();
        }

        var items = new List<ItemListEntry>();
        byte? currentGroup = null;

        foreach (var rawLine in File.ReadLines(itemListPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsGroupHeader(line, out var group))
            {
                currentGroup = group;
                continue;
            }

            if (currentGroup is null)
            {
                continue;
            }

            if (TryParseItemLine(line, currentGroup.Value, out var entry))
            {
                items.Add(entry);
            }
        }

        return items;
    }

    private static bool IsGroupHeader(string line, out byte group)
    {
        group = 0;
        if (!byte.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedGroup))
        {
            return false;
        }

        group = parsedGroup;
        return true;
    }

    private static string GetFirstToken(string line)
    {
        var index = line.IndexOfAny(new[] { ' ', '\t' });
        return index < 0 ? line : line[..index];
    }

    private static bool TryParseItemLine(string line, byte group, out ItemListEntry entry)
    {
        entry = default;
        var columns = line.Split('\t');
        if (columns.Length < 9)
        {
            return false;
        }

        if (!short.TryParse(columns[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        var slotValue = GetInt(columns, 1);
        var width = GetByte(columns, 3);
        var height = GetByte(columns, 4);
        var dropsFromMonsters = GetByte(columns, 7) != 0;
        var name = columns[8].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var slot = ResolveSlot(group, slotValue, name);
        var classStartIndex = GetClassStartIndex(group, columns.Length);
        var (dropLevelIndex, durabilityIndex) = GetDropLevelAndDurabilityIndex(group, classStartIndex);
        var dropLevel = GetByte(columns, dropLevelIndex);
        var durability = GetDurability(group, columns, durabilityIndex);
        var isAmmunition = durability < 0
            || name.Contains("arrow", StringComparison.OrdinalIgnoreCase)
            || name.Contains("bolt", StringComparison.OrdinalIgnoreCase);
        var (wizardClass, knightClass, elfClass, magicGladiatorClass) = GetClassFlags(columns, classStartIndex);
        var (requiredLevel, requiredEnergy, requiredStrength, requiredAgility, value) = GetRequirementValues(group, columns, classStartIndex);

        entry = new ItemListEntry(
            group,
            number,
            slot,
            width,
            height,
            dropsFromMonsters,
            name,
            dropLevel,
            durability,
            isAmmunition,
            wizardClass,
            knightClass,
            elfClass,
            magicGladiatorClass,
            requiredLevel,
            requiredEnergy,
            requiredStrength,
            requiredAgility,
            value);

        return true;
    }

    private static int GetClassStartIndex(byte group, int columnCount)
    {
        if (group == (byte)ItemGroups.Misc2 || columnCount < 20)
        {
            return -1;
        }

        return columnCount - 4;
    }

    private static (int DropLevelIndex, int DurabilityIndex) GetDropLevelAndDurabilityIndex(byte group, int classStartIndex)
    {
        return ((ItemGroups)group) switch
        {
            ItemGroups.Misc1 => (classStartIndex - 7, classStartIndex - 6),
            ItemGroups.Orbs => (9, 11),
            ItemGroups.Shields => (13, 16),
            ItemGroups.Helm => (13, 16),
            ItemGroups.Armor => (13, 16),
            ItemGroups.Pants => (13, 16),
            ItemGroups.Gloves => (13, 16),
            ItemGroups.Boots => (13, 16),
            ItemGroups.Misc2 => (14, -1),
            ItemGroups.Scrolls => (9, -1),
            _ => (13, 17),
        };
    }

    private static (byte WizardClass, byte KnightClass, byte ElfClass, byte MagicGladiatorClass) GetClassFlags(string[] columns, int classStartIndex)
    {
        if (classStartIndex < 0)
        {
            return (0, 0, 0, 0);
        }

        return (
            GetByte(columns, classStartIndex),
            GetByte(columns, classStartIndex + 1),
            GetByte(columns, classStartIndex + 2),
            GetByte(columns, classStartIndex + 3));
    }

    private static (int RequiredLevel, int RequiredEnergy, int RequiredStrength, int RequiredAgility, int Value) GetRequirementValues(byte group, string[] columns, int classStartIndex)
    {
        if (classStartIndex < 0)
        {
            return (0, 0, 0, 0, 0);
        }

        return ((ItemGroups)group) switch
        {
            ItemGroups.Orbs => (
                NormalizeRequirement(GetInt(columns, classStartIndex - 5)),
                NormalizeRequirement(GetInt(columns, classStartIndex - 4)),
                NormalizeRequirement(GetInt(columns, classStartIndex - 3)),
                NormalizeRequirement(GetInt(columns, classStartIndex - 2)),
                NormalizeRequirement(GetInt(columns, classStartIndex - 1))),
            ItemGroups.Scrolls => (
                NormalizeRequirement(GetInt(columns, classStartIndex - 3)),
                NormalizeRequirement(GetInt(columns, classStartIndex - 2)),
                0,
                0,
                NormalizeRequirement(GetInt(columns, classStartIndex - 1))),
            _ => (0, 0, 0, 0, 0),
        };
    }

    private static byte ResolveSlot(byte group, int slotValue, string name)
    {
        if (slotValue > 0)
        {
            return slotValue >= byte.MaxValue ? byte.MaxValue : (byte)slotValue;
        }

        switch ((ItemGroups)group)
        {
            case ItemGroups.Shields:
                return 1;
            case ItemGroups.Helm:
                return 2;
            case ItemGroups.Armor:
                return 3;
            case ItemGroups.Pants:
                return 4;
            case ItemGroups.Gloves:
                return 5;
            case ItemGroups.Boots:
                return 6;
            case ItemGroups.Orbs:
                return name.StartsWith("Wings of", StringComparison.OrdinalIgnoreCase) ? (byte)7 : NoSlot;
            case ItemGroups.Misc1:
                if (IsPet(name))
                {
                    return 8;
                }

                if (IsPendant(name))
                {
                    return 9;
                }

                if (IsRing(name))
                {
                    return 10;
                }

                return NoSlot;
            case ItemGroups.Misc2:
            case ItemGroups.Scrolls:
                return NoSlot;
            default:
                return 0;
        }
    }

    private static bool IsPet(string name)
    {
        return name.StartsWith("Guardian Angel", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Imp", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Horn of", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Dinorant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRing(string name)
    {
        return name.Contains("Ring", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPendant(string name)
    {
        return name.Contains("Pendant", StringComparison.OrdinalIgnoreCase);
    }

    private static byte GetByte(string[] columns, int index)
    {
        var value = GetInt(columns, index);
        if (value <= 0)
        {
            return 0;
        }

        return value >= byte.MaxValue ? byte.MaxValue : (byte)value;
    }

    private static int GetInt(string[] columns, int index)
    {
        if (index < 0 || index >= columns.Length)
        {
            return 0;
        }

        return int.TryParse(columns[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static int NormalizeRequirement(int value)
    {
        return value < 0 ? 0 : value;
    }

    private static int GetDurability(byte group, string[] columns, int durabilityIndex)
    {
        if (durabilityIndex < 0)
        {
            return group == (byte)ItemGroups.Scrolls ? 1 : 0;
        }

        return GetInt(columns, durabilityIndex);
    }
}

internal sealed class ItemList097Filter
{
    private readonly GameConfiguration _gameConfiguration;
    private readonly HashSet<(byte Group, short Number)> _allowedItems;

    public ItemList097Filter(GameConfiguration gameConfiguration)
    {
        this._gameConfiguration = gameConfiguration;
        this._allowedItems = ItemList097.LoadAllowedItems();
    }

    public void Apply()
    {
        if (this._allowedItems.Count == 0)
        {
            return;
        }

        this.FilterDropItemGroups(this._gameConfiguration.DropItemGroups);
        this.FilterMerchantStores(this._gameConfiguration.Monsters);
        this.FilterItemSets(this._gameConfiguration.ItemSetGroups);
    }

    private void FilterDropItemGroups(IEnumerable<DropItemGroup> dropItemGroups)
    {
        foreach (var dropItemGroup in dropItemGroups)
        {
            var itemsToRemove = dropItemGroup.PossibleItems
                .Where(itemDefinition => !this.IsAllowed(itemDefinition))
                .ToList();

            foreach (var itemDefinition in itemsToRemove)
            {
                dropItemGroup.PossibleItems.Remove(itemDefinition);
            }
        }
    }

    private void FilterMerchantStores(IEnumerable<MonsterDefinition> monsters)
    {
        foreach (var monster in monsters)
        {
            var merchantStore = monster.MerchantStore;
            if (merchantStore is null)
            {
                continue;
            }

            var itemsToRemove = merchantStore.Items
                .Where(item => !this.IsAllowed(item.Definition))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                merchantStore.Items.Remove(item);
            }
        }
    }

    private void FilterItemSets(IEnumerable<ItemSetGroup> itemSetGroups)
    {
        foreach (var itemSetGroup in itemSetGroups)
        {
            var itemsToRemove = itemSetGroup.Items
                .Where(itemOfSet => !this.IsAllowed(itemOfSet.ItemDefinition))
                .ToList();

            foreach (var itemOfSet in itemsToRemove)
            {
                itemSetGroup.Items.Remove(itemOfSet);
            }
        }
    }

    private bool IsAllowed(ItemDefinition? itemDefinition)
    {
        return itemDefinition is not null
            && this._allowedItems.Contains((itemDefinition.Group, itemDefinition.Number));
    }
}

internal readonly record struct ItemListEntry(
    byte Group,
    short Number,
    byte Slot,
    byte Width,
    byte Height,
    bool DropsFromMonsters,
    string Name,
    byte DropLevel,
    int Durability,
    bool IsAmmunition,
    byte WizardClass,
    byte KnightClass,
    byte ElfClass,
    byte MagicGladiatorClass,
    int RequiredLevel,
    int RequiredEnergy,
    int RequiredStrength,
    int RequiredAgility,
    int Value);

internal sealed class ItemList097Importer : InitializerBase
{
    private const byte SummonOrbMaximumItemLevel = 3;

    public ItemList097Importer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    public override void Initialize()
    {
        var entries = ItemList097.LoadItems();
        if (entries.Count == 0)
        {
            return;
        }

        var existingItems = new HashSet<(byte Group, short Number)>(
            this.GameConfiguration.Items.Select(item => (item.Group, item.Number)));

        foreach (var entry in entries)
        {
            var suppressMonsterDrop = ShouldSuppressMonsterDrop(entry);
            if (existingItems.Contains((entry.Group, entry.Number)))
            {
                var existingItem = this.GameConfiguration.Items.First(item => item.Group == entry.Group && item.Number == entry.Number);
                if (IsSkillEntry(entry))
                {
                    this.ApplySkillItemOverrides(existingItem, entry);
                }

                if (existingItem.ItemSlot is null && entry.Slot != byte.MaxValue)
                {
                    existingItem.ItemSlot = this.GetSlotType(entry);
                }

                if (existingItem.Group == (byte)ItemGroups.Misc1
                    && existingItem.Number == 20
                    && entry.Durability > 0)
                {
                    existingItem.Durability = (byte)Math.Min(byte.MaxValue, entry.Durability);
                }

                if (suppressMonsterDrop)
                {
                    existingItem.DropsFromMonsters = false;
                }

                this.UpdateQualifiedCharacters(existingItem, entry, overwrite: IsSkillEntry(entry));

                continue;
            }

            var item = this.Context.CreateNew<ItemDefinition>();
            item.Name = entry.Name;
            item.Group = entry.Group;
            item.Number = entry.Number;
            item.Width = entry.Width;
            item.Height = entry.Height;
            item.DropsFromMonsters = entry.DropsFromMonsters && !suppressMonsterDrop;
            item.DropLevel = entry.DropLevel;
            item.Durability = entry.Durability < 0 ? (byte)255 : (byte)Math.Min(byte.MaxValue, entry.Durability);
            item.IsAmmunition = entry.IsAmmunition;
            item.MaximumItemLevel = GetMaximumItemLevel(entry);
            item.ItemSlot = this.GetSlotType(entry);
            item.SetGuid(item.Group, item.Number);
            if (!IsSkillEntry(entry))
            {
                this.ApplyTemplateOptions(item);
            }

            this.UpdateQualifiedCharacters(item, entry, overwrite: false);
            this.ApplySkillItemOverrides(item, entry);

            this.GameConfiguration.Items.Add(item);
            existingItems.Add((entry.Group, entry.Number));
        }
    }

    private void ApplySkillItemOverrides(ItemDefinition item, ItemListEntry entry)
    {
        if (!IsSkillEntry(entry))
        {
            return;
        }

        var maximumItemLevel = GetMaximumItemLevel(entry);
        if (item.MaximumItemLevel != maximumItemLevel)
        {
            item.MaximumItemLevel = maximumItemLevel;
        }

        if (item.PossibleItemOptions.Count > 0)
        {
            item.PossibleItemOptions.Clear();
        }

        if (entry.Value > 0)
        {
            item.Value = entry.Value;
        }

        this.AssignSkillIfMissing(item, entry);
        this.UpdateItemRequirements(item, entry);
    }

    private ItemSlotType? GetSlotType(ItemListEntry entry)
    {
        if (entry.Slot == byte.MaxValue)
        {
            return null;
        }

        if (entry.Slot <= 1 && entry.Group <= (byte)ItemGroups.Staff)
        {
            var dualSlot = this.GameConfiguration.ItemSlotTypes
                .FirstOrDefault(type => type.ItemSlots.Contains(0) && type.ItemSlots.Contains(1));
            if (dualSlot is not null)
            {
                return dualSlot;
            }
        }

        return this.GameConfiguration.ItemSlotTypes
            .FirstOrDefault(type => type.ItemSlots.Contains(entry.Slot));
    }

    private void ApplyTemplateOptions(ItemDefinition item)
    {
        if (item.PossibleItemOptions.Count > 0)
        {
            return;
        }

        var template = this.GameConfiguration.Items
            .FirstOrDefault(existing => existing.Group == item.Group && existing.PossibleItemOptions.Count > 0);
        if (template is null)
        {
            return;
        }

        foreach (var option in template.PossibleItemOptions)
        {
            item.PossibleItemOptions.Add(option);
        }
    }

    private static byte GetMaximumItemLevel(ItemListEntry entry)
    {
        if (entry.IsAmmunition)
        {
            return 0;
        }

        if (IsSummonOrbEntry(entry))
        {
            return SummonOrbMaximumItemLevel;
        }

        if (IsSkillEntry(entry))
        {
            return 0;
        }

        if (IsNonUpgradeableMiscItem(entry))
        {
            return 0;
        }

        if (IsNonUpgradeableConsumable(entry.Name))
        {
            return 0;
        }

        return Version095dItems.Constants.MaximumItemLevel;
    }

    private static bool ShouldSuppressMonsterDrop(ItemListEntry entry)
    {
        if (entry.Group != (byte)ItemGroups.Misc2)
        {
            return false;
        }

        return entry.Number is 15 or 20 or 22 or 23 or 24 or 25 or 26;
    }

    private static bool IsNonUpgradeableMiscItem(ItemListEntry entry)
    {
        if (entry.Group != (byte)ItemGroups.Misc2)
        {
            return false;
        }

        return entry.Number is 15 or 20 or 21 or 22 or 23 or 24 or 25 or 26;
    }

    private static bool IsNonUpgradeableConsumable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Contains("Orb of", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Jewel of", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Scroll of", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOrbEntry(ItemListEntry entry)
    {
        return entry.Group == (byte)ItemGroups.Orbs
            && entry.Name.Contains("Orb", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsScrollEntry(ItemListEntry entry)
    {
        return entry.Group == (byte)ItemGroups.Scrolls;
    }

    private static bool IsSkillEntry(ItemListEntry entry)
    {
        return IsOrbEntry(entry) || IsScrollEntry(entry);
    }

    private static bool IsSummonOrbEntry(ItemListEntry entry)
    {
        return entry.Group == (byte)ItemGroups.Orbs && entry.Number == 11;
    }

    private void UpdateQualifiedCharacters(ItemDefinition item, ItemListEntry entry, bool overwrite)
    {
        if (entry.WizardClass == 0 && entry.KnightClass == 0 && entry.ElfClass == 0 && entry.MagicGladiatorClass == 0)
        {
            return;
        }

        if (overwrite)
        {
            item.QualifiedCharacters.Clear();
        }
        else if (item.QualifiedCharacters.Count > 0)
        {
            return;
        }

        var qualifiedClasses = this.GameConfiguration.DetermineCharacterClassesByRank(
            entry.WizardClass,
            entry.KnightClass,
            entry.ElfClass,
            entry.MagicGladiatorClass,
            0,
            0,
            0);
        qualifiedClasses.ToList().ForEach(item.QualifiedCharacters.Add);
    }

    private void AssignSkillIfMissing(ItemDefinition item, ItemListEntry entry)
    {
        if (item.Skill is not null)
        {
            return;
        }

        if (!TryGetSkillNumber(entry, out var skillNumber))
        {
            return;
        }

        item.Skill = this.GameConfiguration.Skills.FirstOrDefault(skill => skill.Number == (short)skillNumber);
    }

    private void UpdateItemRequirements(ItemDefinition item, ItemListEntry entry)
    {
        if (!IsSkillEntry(entry))
        {
            return;
        }

        this.RemoveRequirement(item, Stats.Level);
        this.RemoveRequirement(item, Stats.TotalStrength);
        this.RemoveRequirement(item, Stats.TotalAgility);
        this.RemoveRequirement(item, GetEnergyRequirementAttribute(entry));

        this.CreateItemRequirementIfNeeded(item, Stats.Level, entry.RequiredLevel);
        this.CreateItemRequirementIfNeeded(item, Stats.TotalStrength, entry.RequiredStrength);
        this.CreateItemRequirementIfNeeded(item, Stats.TotalAgility, entry.RequiredAgility);
        this.CreateItemRequirementIfNeeded(item, GetEnergyRequirementAttribute(entry), entry.RequiredEnergy);
    }

    private AttributeDefinition GetEnergyRequirementAttribute(ItemListEntry entry)
    {
        return IsScrollEntry(entry) ? Stats.TotalEnergyRequirementValue : Stats.TotalEnergy;
    }

    private void RemoveRequirement(ItemDefinition item, AttributeDefinition attribute)
    {
        var persistentAttribute = attribute.GetPersistent(this.GameConfiguration);
        var toRemove = item.Requirements.Where(requirement => requirement.Attribute == persistentAttribute).ToList();
        foreach (var requirement in toRemove)
        {
            item.Requirements.Remove(requirement);
        }
    }

    private static bool TryGetSkillNumber(ItemListEntry entry, out SkillNumber skillNumber)
    {
        if (entry.Group == (byte)ItemGroups.Orbs)
        {
            skillNumber = entry.Number switch
            {
                7 => SkillNumber.TwistingSlash,
                8 => SkillNumber.Heal,
                9 => SkillNumber.GreaterDefense,
                10 => SkillNumber.GreaterDamage,
                11 => SkillNumber.SummonGoblin,
                12 => SkillNumber.RagefulBlow,
                13 => SkillNumber.Impale,
                14 => SkillNumber.SwellLife,
                16 => SkillNumber.FireSlash,
                17 => SkillNumber.Penetration,
                18 => SkillNumber.IceArrow,
                19 => SkillNumber.DeathStab,
                _ => default,
            };
            return skillNumber != default;
        }

        if (entry.Group == (byte)ItemGroups.Scrolls)
        {
            skillNumber = entry.Number switch
            {
                0 => SkillNumber.Poison,
                1 => SkillNumber.Meteorite,
                2 => SkillNumber.Lightning,
                3 => SkillNumber.FireBall,
                4 => SkillNumber.Flame,
                5 => SkillNumber.Teleport,
                6 => SkillNumber.Ice,
                7 => SkillNumber.Twister,
                8 => SkillNumber.EvilSpirit,
                9 => SkillNumber.Hellfire,
                10 => SkillNumber.PowerWave,
                11 => SkillNumber.AquaBeam,
                12 => SkillNumber.Cometfall,
                13 => SkillNumber.Inferno,
                14 => SkillNumber.TeleportAlly,
                15 => SkillNumber.SoulBarrier,
                16 => SkillNumber.Decay,
                17 => SkillNumber.IceStorm,
                18 => SkillNumber.Nova,
                _ => default,
            };
            return skillNumber != default;
        }

        skillNumber = default;
        return false;
    }
}
