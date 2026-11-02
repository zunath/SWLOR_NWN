namespace SWLOR.Game.Server.Service.EnzymeCultureService
{
    /// <summary>Why a Fermentation Vat action cannot be done. <see cref="None"/> means it can.</summary>
    public enum FermentationRefusal
    {
        None = 0,

        /// <summary>Only characters can use a vat.</summary>
        NotAPlayer = 1,

        /// <summary>The vat is not there any more.</summary>
        NoVat = 2,

        /// <summary>The character lacks garden permission for the house the vat stands in.</summary>
        NoAccess = 3,

        /// <summary>The type and color are not one of the 15 enzymes.</summary>
        UnknownEnzyme = 4,

        /// <summary>The vat already holds a batch, running or waiting to be collected.</summary>
        VatBusy = 5,

        /// <summary>The CD key already has a batch running in a vat.</summary>
        CdKeyBusy = 6,

        /// <summary>Agriculture has not reached level 20.</summary>
        SkillTooLow = 7,

        /// <summary>The item picked is not a crop a culture can use.</summary>
        NotACrop = 8,

        /// <summary>The character carries fewer than 2 of the crop.</summary>
        NoCrops = 9,

        /// <summary>The character carries no manure.</summary>
        NoManure = 10,

        /// <summary>The character carries no Distilled Water.</summary>
        NoWater = 11,

        /// <summary>The vat has not copied this color yet, and the character carries no hunted enzyme of it to start from.</summary>
        NoStarter = 12,

        /// <summary>The vat holds no batch.</summary>
        NoBatch = 13,

        /// <summary>The batch is another character's.</summary>
        NotTheOwner = 14,

        /// <summary>The batch has not finished.</summary>
        NotDone = 15,
    }
}
