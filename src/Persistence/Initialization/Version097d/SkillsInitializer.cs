// <copyright file="SkillsInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Version097d;

using System;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// Initialization logic for skills of version 0.97d.
/// </summary>
internal class SkillsInitializer : MUnique.OpenMU.Persistence.Initialization.Version095d.SkillsInitializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SkillsInitializer"/> class.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public SkillsInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        if (!this.GameConfiguration.Skills.Any(s => s.Number == (short)SkillNumber.CrescentMoonSlash))
        {
            var knightClasses = CharacterClasses.DarkKnight | CharacterClasses.MagicGladiator;
            this.CreateSkill(
                SkillNumber.CrescentMoonSlash,
                "Crescent Moon Slash",
                knightClasses,
                DamageType.Physical,
                90,
                4,
                abilityConsumption: 15,
                manaConsumption: 22,
                movesToTarget: true,
                movesTarget: true);
        }

        this.InitializeSecondClassSkills();
        this.InitializeSkillEffects();
    }

    private void InitializeSecondClassSkills()
    {
        var wizardClasses = CharacterClasses.DarkWizard | CharacterClasses.SoulMaster;
        var knightClasses = CharacterClasses.DarkKnight | CharacterClasses.BladeKnight;
        var elfClasses = CharacterClasses.FairyElf | CharacterClasses.MuseElf;

        this.CreateSkillIfMissing(
            SkillNumber.TeleportAlly,
            () => this.CreateSkill(
                SkillNumber.TeleportAlly,
                "Teleport Ally",
                wizardClasses,
                distance: 6,
                abilityConsumption: 25,
                manaConsumption: 90,
                energyRequirement: 188,
                skillType: SkillType.Other));

        this.CreateSkillIfMissing(
            SkillNumber.SoulBarrier,
            () => this.CreateSkill(
                SkillNumber.SoulBarrier,
                "Soul Barrier",
                wizardClasses,
                distance: 6,
                abilityConsumption: 22,
                manaConsumption: 70,
                energyRequirement: 126,
                skillType: SkillType.Buff,
                implicitTargetRange: 0,
                targetRestriction: SkillTargetRestriction.Party));

        this.CreateSkillIfMissing(
            SkillNumber.Decay,
            () => this.CreateSkill(
                SkillNumber.Decay,
                "Decay",
                wizardClasses,
                DamageType.Wizardry,
                95,
                6,
                abilityConsumption: 7,
                manaConsumption: 110,
                energyRequirement: 243,
                elementalModifier: ElementalType.Poison,
                skillType: SkillType.AreaSkillAutomaticHits));

        if (this.CreateSkillIfMissing(
                SkillNumber.IceStorm,
                () => this.CreateSkill(
                    SkillNumber.IceStorm,
                    "Ice Storm",
                    wizardClasses,
                    DamageType.Wizardry,
                    80,
                    6,
                    abilityConsumption: 5,
                    manaConsumption: 100,
                    energyRequirement: 223,
                    elementalModifier: ElementalType.Ice,
                    skillType: SkillType.AreaSkillAutomaticHits)))
        {
            this.AddAreaSkillSettings(
                SkillNumber.IceStorm,
                false,
                default,
                default,
                default,
                useDeferredHits: true,
                delayPerOneDistance: TimeSpan.Zero,
                delayBetweenHits: TimeSpan.FromMilliseconds(200),
                targetAreaDiameter: 3,
                useTargetAreaFilter: true);
        }

        this.CreateSkillIfMissing(
            SkillNumber.Nova,
            () => this.CreateSkill(
                SkillNumber.Nova,
                "Nova",
                wizardClasses,
                DamageType.Wizardry,
                distance: 6,
                manaConsumption: 15,
                elementalModifier: ElementalType.Fire));

        this.CreateSkillIfMissing(
            SkillNumber.NovaStart,
            () => this.CreateSkill(
                SkillNumber.NovaStart,
                "Nova (Start)",
                wizardClasses,
                DamageType.None,
                abilityConsumption: 45,
                skillType: SkillType.Other));

        this.CreateSkillIfMissing(
            SkillNumber.RagefulBlow,
            () => this.CreateSkill(
                SkillNumber.RagefulBlow,
                "Rageful Blow",
                knightClasses,
                DamageType.Physical,
                60,
                3,
                abilityConsumption: 20,
                manaConsumption: 25,
                levelRequirement: 170,
                elementalModifier: ElementalType.Earth,
                skillType: SkillType.AreaSkillAutomaticHits));

        this.CreateSkillIfMissing(
            SkillNumber.DeathStab,
            () => this.CreateSkill(
                SkillNumber.DeathStab,
                "Death Stab",
                knightClasses,
                DamageType.Physical,
                70,
                2,
                abilityConsumption: 12,
                manaConsumption: 15,
                levelRequirement: 160,
                elementalModifier: ElementalType.Wind,
                skillType: SkillType.DirectHit,
                skillTarget: SkillTarget.ExplicitWithImplicitInRange,
                implicitTargetRange: 1));

        this.CreateSkillIfMissing(
            SkillNumber.SwellLife,
            () => this.CreateSkill(
                SkillNumber.SwellLife,
                "Swell Life",
                knightClasses,
                abilityConsumption: 24,
                manaConsumption: 22,
                levelRequirement: 120,
                skillType: SkillType.Buff,
                skillTarget: SkillTarget.ImplicitParty));

        this.CreateSkillIfMissing(
            SkillNumber.IceArrow,
            () => this.CreateSkill(
                SkillNumber.IceArrow,
                "Ice Arrow",
                elfClasses,
                DamageType.Physical,
                105,
                8,
                abilityConsumption: 12,
                manaConsumption: 10,
                elementalModifier: ElementalType.Ice));

        if (this.CreateSkillIfMissing(
                SkillNumber.Penetration,
                () => this.CreateSkill(
                    SkillNumber.Penetration,
                    "Penetration",
                    elfClasses,
                    DamageType.Physical,
                    70,
                    6,
                    abilityConsumption: 9,
                    manaConsumption: 7,
                    levelRequirement: 130,
                    elementalModifier: ElementalType.Wind,
                    skillType: SkillType.AreaSkillAutomaticHits)))
        {
            this.AddAreaSkillSettings(
                SkillNumber.Penetration,
                true,
                1.1f,
                1.2f,
                8f,
                useDeferredHits: true,
                delayPerOneDistance: TimeSpan.FromMilliseconds(50));
        }

        if (this.CreateSkillIfMissing(
                SkillNumber.FireSlash,
                () => this.CreateSkill(
                    SkillNumber.FireSlash,
                    "Fire Slash",
                    CharacterClasses.MagicGladiator,
                    DamageType.Physical,
                    80,
                    2,
                    abilityConsumption: 20,
                    manaConsumption: 15,
                    elementalModifier: ElementalType.Fire,
                    skillType: SkillType.AreaSkillAutomaticHits)))
        {
            this.AddAreaSkillSettings(SkillNumber.FireSlash, true, 1.5f, 2f, 2f);
        }
    }

    private void InitializeSkillEffects()
    {
        this.EnsureMagicEffect(MagicEffectNumber.SoulBarrier, () => new SoulBarrierEffectInitializer(this.Context, this.GameConfiguration).Initialize());
        this.EnsureMagicEffect(MagicEffectNumber.GreaterFortitude, () => new LifeSwellEffectInitializer(this.Context, this.GameConfiguration).Initialize());

        this.AssignSkillEffect(SkillNumber.SoulBarrier, MagicEffectNumber.SoulBarrier);
        this.AssignSkillEffect(SkillNumber.SwellLife, MagicEffectNumber.GreaterFortitude);
    }

    private bool CreateSkillIfMissing(SkillNumber skillNumber, Action createSkill)
    {
        if (this.GameConfiguration.Skills.Any(s => s.Number == (short)skillNumber))
        {
            return false;
        }

        createSkill();
        return true;
    }

    private void EnsureMagicEffect(MagicEffectNumber effectNumber, Action initializeEffect)
    {
        if (this.GameConfiguration.MagicEffects.Any(e => e.Number == (short)effectNumber))
        {
            return;
        }

        initializeEffect();
    }

    private void AssignSkillEffect(SkillNumber skillNumber, MagicEffectNumber effectNumber)
    {
        var skill = this.GameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)skillNumber);
        if (skill is null)
        {
            return;
        }

        var effect = this.GameConfiguration.MagicEffects.FirstOrDefault(e => e.Number == (short)effectNumber);
        if (effect is null)
        {
            return;
        }

        skill.MagicEffectDef = effect;
    }
}
