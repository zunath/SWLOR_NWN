using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    public class SituationBuilder
    {
        private readonly Dictionary<SituationType, SituationDetail> _situations = new();
        private SituationDetail _situation;
        private SituationChoice _choice;

        /// <summary>Starts a situation. This should be called first.</summary>
        public SituationBuilder Create(SituationType type)
        {
            _situation = new SituationDetail(type);
            _choice = null;
            _situations[type] = _situation;

            return this;
        }

        public SituationBuilder Title(string title)
        {
            _situation.Title = title;
            return this;
        }

        public SituationBuilder Text(string text)
        {
            _situation.Text = text;
            return this;
        }

        public SituationBuilder Weight(int weight)
        {
            _situation.Weight = weight;
            return this;
        }

        /// <summary>The situation only arrives as the follow-up of another decision.</summary>
        public SituationBuilder FollowUpOnly()
        {
            _situation.IsFollowUp = true;
            return this;
        }

        /// <summary>An event elsewhere in the game raises the situation, so it is never drawn at random.</summary>
        public SituationBuilder EventOnly()
        {
            _situation.IsEventDriven = true;
            return this;
        }

        /// <summary>The situation exposes the secret Gazette entry it carries when it arrives.</summary>
        public SituationBuilder ExposesSecret()
        {
            _situation.ExposesSecret = true;
            return this;
        }

        /// <summary>A condition the city must meet for the situation to be drawn.</summary>
        public SituationBuilder Requires(SituationRequirement requirement)
        {
            _situation.Requirements.Add(requirement);
            return this;
        }

        /// <summary>Starts a choice. The choice calls that follow apply to it.</summary>
        public SituationBuilder AddChoice(string text)
        {
            _choice = new SituationChoice(text);
            _situation.Choices.Add(_choice);
            return this;
        }

        /// <summary>A condition the city or the official must meet to pick the choice.</summary>
        public SituationBuilder ChoiceRequires(SituationRequirement requirement)
        {
            _choice.Requirements.Add(requirement);
            return this;
        }

        public SituationBuilder Pay(int credits)
        {
            _choice.Effects.Add(SituationEffect.TreasuryChange(-credits));
            return this;
        }

        public SituationBuilder Gain(int credits)
        {
            _choice.Effects.Add(SituationEffect.TreasuryChange(credits));
            return this;
        }

        public SituationBuilder Modifier(CityModifierType modifier)
        {
            _choice.Effects.Add(SituationEffect.AddModifier(modifier));
            return this;
        }

        /// <summary>The choice calls a hunt: an NPC wave attacks the city's corrals for 30 minutes.</summary>
        public SituationBuilder CallHunt()
        {
            _choice.Effects.Add(SituationEffect.CallHunt());
            return this;
        }

        /// <summary>The choice leaves the city's animals giving no products for 72 hours.</summary>
        public SituationBuilder LivestockLoseProducts()
        {
            _choice.Effects.Add(SituationEffect.LivestockLoseProducts());
            return this;
        }

        /// <summary>The choice sends the city's help to the holding the situation is about, ending its raid.</summary>
        public SituationBuilder SendHelp()
        {
            _choice.Effects.Add(SituationEffect.SendHoldingHelp());
            return this;
        }

        /// <summary>The choice pays the customs fine of the holding the situation is about from the treasury, up to the cost limit.</summary>
        public SituationBuilder PayHoldingFine()
        {
            _choice.Effects.Add(SituationEffect.PayHoldingFine());
            return this;
        }

        public SituationBuilder FollowUp(SituationType followUp, int delayDays, int oneIn = 1, bool carriesSecret = false)
        {
            _choice.Effects.Add(SituationEffect.QueueFollowUp(followUp, delayDays, oneIn, carriesSecret));
            return this;
        }

        /// <summary>The choice's Gazette entry stays out of the public log until a later situation exposes it.</summary>
        public SituationBuilder Secret()
        {
            _choice.IsSecret = true;
            return this;
        }

        /// <summary>The choice's Gazette entry also goes to the DM feed.</summary>
        public SituationBuilder Hook()
        {
            _choice.IsHook = true;
            return this;
        }

        public SituationBuilder Gazette(string text)
        {
            _choice.GazetteText = text;
            return this;
        }

        public SituationBuilder IgnoredPays(int credits)
        {
            _situation.IgnoredEffects.Add(SituationEffect.TreasuryChange(-credits));
            return this;
        }

        public SituationBuilder IgnoredModifier(CityModifierType modifier)
        {
            _situation.IgnoredEffects.Add(SituationEffect.AddModifier(modifier));
            return this;
        }

        public SituationBuilder IgnoredLivestockLoseProducts()
        {
            _situation.IgnoredEffects.Add(SituationEffect.LivestockLoseProducts());
            return this;
        }

        public SituationBuilder IgnoredFollowUp(SituationType followUp, int delayDays)
        {
            _situation.IgnoredEffects.Add(SituationEffect.QueueFollowUp(followUp, delayDays));
            return this;
        }

        public SituationBuilder IgnoredHook()
        {
            _situation.IgnoredIsHook = true;
            return this;
        }

        public SituationBuilder IgnoredGazette(string text)
        {
            _situation.IgnoredGazetteText = text;
            return this;
        }

        public Dictionary<SituationType, SituationDetail> Build()
        {
            return _situations;
        }
    }
}
