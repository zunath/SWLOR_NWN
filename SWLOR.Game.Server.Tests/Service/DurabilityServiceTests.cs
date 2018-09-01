using System;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Tests.Service
{
    public class DurabilityServiceTests
    {
        [Test]
        public void DurabilityService_IsValidDurabilityType_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.IsValidDurabilityType(null);
            });
        }

        [Test]
        public void DurabilityService_IsValidDurabilityType_ShouldBeTrue()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(NWScript.BASE_ITEM_ARMOR);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BASTARDSWORD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BATTLEAXE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BELT);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BOOTS);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BRACER);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CLOAK);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CLUB);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_DAGGER);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_DIREMACE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_DOUBLEAXE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_DWARVENWARAXE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_GLOVES);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_GREATAXE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_GREATSWORD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_HALBERD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_HANDAXE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_HEAVYCROSSBOW);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_HEAVYFLAIL);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_HELMET);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_KAMA);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_KATANA);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_KUKRI);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LARGESHIELD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LIGHTCROSSBOW);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LIGHTFLAIL);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LIGHTHAMMER);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LIGHTMACE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LONGBOW);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LONGSWORD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MORNINGSTAR);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_QUARTERSTAFF);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_RAPIER);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SCIMITAR);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SCYTHE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SHORTBOW);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SHORTSPEAR);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SHORTSWORD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SICKLE);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SLING);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SMALLSHIELD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_TOWERSHIELD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_TRIDENT);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_TWOBLADEDSWORD);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_WARHAMMER);
            Assert.IsTrue(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_WHIP);
            Assert.IsTrue(service.IsValidDurabilityType(item));
        }

        [Test]
        public void DurabilityService_IsValidDurabilityType_ShouldBeFalse()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(NWScript.BASE_ITEM_TORCH);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_AMULET);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_ARROW);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MISCSMALL);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BOLT);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BULLET);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MISCMEDIUM);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_DART);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MISCLARGE);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_HEALERSKIT);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MISCTALL);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MAGICROD);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MAGICSTAFF);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MAGICWAND);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_POTIONS);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_RING);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SCROLL);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SHURIKEN);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_THIEVESTOOLS);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_TRAPKIT);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_KEY);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_LARGEBOX);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MISCWIDE);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CSLASHWEAPON);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CPIERCWEAPON);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CBLUDGWEAPON);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CSLSHPRCWEAP);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CREATUREITEM);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BOOK);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_SPELLSCROLL);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_GOLD);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_GEM);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_MISCTHIN);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_GRENADE);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BLANK_POTION);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BLANK_SCROLL);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_BLANK_WAND);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_ENCHANTED_POTION);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_ENCHANTED_SCROLL);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_ENCHANTED_WAND);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CRAFTMATERIALMED);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_CRAFTMATERIALSML);
            Assert.IsFalse(service.IsValidDurabilityType(item));
            item.BaseItemType.Returns(NWScript.BASE_ITEM_INVALID);
            Assert.IsFalse(service.IsValidDurabilityType(item));
        }

        [Test]
        public void DurabilityService_GetMaxDurability_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.GetMaxDurability(null);
            });
        }

        [Test]
        public void DurabilityService_SetMaxDurability_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.SetMaxDurability(null, 0.0f);
            });
        }

        [Test]
        public void DurabilityService_GetDurability_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.GetDurability(null);
            });
        }

        [Test]
        public void DurabilityService_SetDurability_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.SetMaxDurability(null, 0.0f);
            });
        }

        [Test]
        public void DurabilityService_GetMaxDurability_InvalidType_ShouldReturnNegative1()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_BLANK_SCROLL);

            float result = service.GetMaxDurability(item);
            Assert.AreEqual(-1.0f, result);
        }

        [Test]
        public void DurabilityService_GetMaxDurability_ShouldReturnDefault()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);

            float result = service.GetMaxDurability(item);
            Assert.AreEqual(30.0f, result);
        }

        [Test]
        public void DurabilityService_GetMaxDurability_ShouldReturn4()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);
            item.GetLocalInt(Arg.Any<string>()).Returns(4);

            float result = service.GetMaxDurability(item);
            Assert.AreEqual(4, result);
        }

        [Test]
        public void DurabilityService_GetDurability_ShouldReturn6Point23()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);
            item.GetLocalFloat(Arg.Any<string>()).Returns(6.23f);

            float result = service.GetDurability(item);
            Assert.AreEqual(6.23f, result);
        }

        [Test]
        public void DurabilityService_SetMaxDurability_InvalidType_ShouldNotRunOnce()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            bool ranOnce = false;
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_BLANK_SCROLL);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => ranOnce = true);

            service.SetMaxDurability(item, 999.0f);
            Assert.AreEqual(false, ranOnce);
        }

        [Test]
        public void DurabilityService_SetMaxDurability_ShouldSetToDefaultValue()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetMaxDurability(item, -50.0f);
            Assert.AreEqual(30.0f, value);
        }

        [Test]
        public void DurabilityService_SetMaxDurability_ShouldSetToSpecifiedValue()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat("DURABILITY_MAX", Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetMaxDurability(item, 12.52f);
            Assert.AreEqual(12.52f, value);
        }


        [Test]
        public void DurabilityService_SetDurability_InvalidType_ShouldNotRunOnce()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            bool ranOnce = false;
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_BLANK_SCROLL);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => ranOnce = true);

            service.SetDurability(item, 999.0f);
            Assert.AreEqual(false, ranOnce);
        }

        [Test]
        public void DurabilityService_SetDurability_ShouldSetToDefaultValue()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetDurability(item, -50.0f);
            Assert.AreEqual(0.0f, value);
        }

        [Test]
        public void DurabilityService_SetDurability_ShouldSetToSpecifiedValue()
        {
            INWScript script = Substitute.For<INWScript>();
            IColorTokenService color = Substitute.For<IColorTokenService>();
            DurabilityService service = new DurabilityService(script, color);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(script, service);
            item.BaseItemType.Returns(x => NWScript.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat("DURABILITY_CURRENT", Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetDurability(item, 12.52f);
            Assert.AreEqual(12.52f, value);
        }
    }
}
