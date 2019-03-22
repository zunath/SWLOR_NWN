﻿using System;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.Tests.Service
{
    public class DurabilityServiceTests
    {
        [Test]
        public void DurabilityService_GetMaxDurability_InvalidArguments_ShouldThrowException()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler,creature);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.GetMaxDurability(null);
            });
        }

        [Test]
        public void DurabilityService_SetMaxDurability_InvalidArguments_ShouldThrowException()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.SetMaxDurability(null, 0.0f);
            });
        }

        [Test]
        public void DurabilityService_GetDurability_InvalidArguments_ShouldThrowException()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.GetDurability(null);
            });
        }

        [Test]
        public void DurabilityService_SetDurability_InvalidArguments_ShouldThrowException()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.SetMaxDurability(null, 0.0f);
            });
        }

        [Test]
        public void DurabilityService_GetMaxDurability_InvalidType_ShouldReturnNegative1()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_BLANK_SCROLL);

            float result = service.GetMaxDurability(item);
            Assert.AreEqual(-1.0f, result);
        }

        [Test]
        public void DurabilityService_GetMaxDurability_ShouldReturnDefault()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);

            float result = service.GetMaxDurability(item);
            Assert.AreEqual(30.0f, result);
        }

        [Test]
        public void DurabilityService_GetMaxDurability_ShouldReturn4()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);
            item.GetLocalInt(Arg.Any<string>()).Returns(4);

            float result = service.GetMaxDurability(item);
            Assert.AreEqual(4, result);
        }

        [Test]
        public void DurabilityService_GetDurability_ShouldReturn6Point23()
        {
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);
            item.GetLocalFloat(Arg.Any<string>()).Returns(6.23f);

            float result = service.GetDurability(item);
            Assert.AreEqual(6.23f, result);
        }

        [Test]
        public void DurabilityService_SetMaxDurability_InvalidType_ShouldNotRunOnce()
        {
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            bool ranOnce = false;
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_BLANK_SCROLL);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => ranOnce = true);

            service.SetMaxDurability(item, 999.0f);
            Assert.AreEqual(false, ranOnce);
        }

        [Test]
        public void DurabilityService_SetMaxDurability_ShouldSetToDefaultValue()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetMaxDurability(item, -50.0f);
            Assert.AreEqual(30.0f, value);
        }

        [Test]
        public void DurabilityService_SetMaxDurability_ShouldSetToSpecifiedValue()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat("DURABILITY_MAX", Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetMaxDurability(item, 12.52f);
            Assert.AreEqual(12.52f, value);
        }


        [Test]
        public void DurabilityService_SetDurability_InvalidType_ShouldNotRunOnce()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            bool ranOnce = false;
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_BLANK_SCROLL);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => ranOnce = true);

            service.SetDurability(item, 999.0f);
            Assert.AreEqual(false, ranOnce);
        }

        [Test]
        public void DurabilityService_SetDurability_ShouldSetToDefaultValue()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat(Arg.Any<string>(), Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetDurability(item, -50.0f);
            Assert.AreEqual(0.0f, value);
        }

        [Test]
        public void DurabilityService_SetDurability_ShouldSetToSpecifiedValue()
        {
            
            IColorTokenService color = Substitute.For<IColorTokenService>();
            INWNXProfiler nwnxProfiler = Substitute.For<INWNXProfiler>();
            INWNXCreature creature = Substitute.For<INWNXCreature>();
            DurabilityService service = new DurabilityService(color, nwnxProfiler, creature);
            float value = 0.0f;
            NWItem item = Substitute.For<NWItem>(service);
            item.BaseItemType.Returns(x => _.BASE_ITEM_LONGSWORD);
            item.When(x => x.SetLocalFloat("DURABILITY_CURRENT", Arg.Any<float>()))
                .Do(x => value = x.ArgAt<float>(1));

            service.SetDurability(item, 12.52f);
            Assert.AreEqual(12.52f, value);
        }
    }
}
