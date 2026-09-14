using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class GuiWidgetEngineTests
    {
        [EngineTest("GUI encouraged styles serialize static values and dynamic bindings", Category = "AppearanceEditor", TimeoutSeconds = 10f)]
        public static Task EncouragedStylesSerializeNativeJson(EngineTestContext ctx)
        {
            AssertEncouragedStyles(ctx, () => new GuiButton<EncouragedModel>()
                .SetId("encouraged_button")
                .SetText("Select")
                .SetWidth(90f)
                .SetHeight(28f)
                .BindIsEnabled(model => model.Enabled));
            AssertEncouragedStyles(ctx, () => new GuiImage<EncouragedModel>()
                .SetId("encouraged_image")
                .SetResref("gui_pal_tattoo")
                .SetAspect(NuiAspect.Stretch)
                .SetRegion(new GuiRectangle(16f, 32f, 16f, 16f))
                .SetMargin(2f)
                .BindIsVisible(model => model.Enabled));
            ctx.SetResultDetail("Native JSON verified unset/false/true encouragement, true-to-false reset, dynamic references, static/bound precedence in both setter orders and rebinding for buttons and images. All other serialized widget fields stayed unchanged. No client window or rendered glow is exercised.");
            return Task.CompletedTask;
        }

        private static void AssertEncouragedStyles<TWidget>(EngineTestContext ctx, Func<TWidget> create)
            where TWidget : GuiWidget<EncouragedModel, TWidget>
        {
            var baseline = Serialize(create());
            var type = baseline["type"].Value<string>();
            ctx.Assert(baseline["encouraged"] == null, $"{type}: ordinary widgets keep the native unencouraged default.");
            ctx.Assert(JToken.DeepEquals(baseline, Serialize(create().SetIsEncouraged(false))),
                $"{type}: explicit false must not alter unrelated JSON or enable encouragement.");

            var widget = create().SetIsEncouraged(true);
            var encouraged = Serialize(widget);
            ctx.AssertEqual(JTokenType.Boolean, encouraged["encouraged"]?.Type, $"{type}: static encouragement has boolean type");
            ctx.AssertEqual(true, encouraged["encouraged"]?.Value<bool>(), $"{type}: static true reaches the native serializer");
            AssertOnlyEncouragedChanged(ctx, baseline, encouraged, type);
            widget.SetIsEncouraged(false);
            ctx.Assert(JToken.DeepEquals(baseline, Serialize(widget)), $"{type}: true-to-false must clear the static style.");

            AssertBinding(ctx, baseline, Serialize(create().BindIsEncouraged(model => model.Selected)),
                nameof(EncouragedModel.Selected), type);
            AssertBinding(ctx, baseline, Serialize(create().SetIsEncouraged(true).BindIsEncouraged(model => model.Selected)),
                nameof(EncouragedModel.Selected), type);
            AssertBinding(ctx, baseline, Serialize(create().BindIsEncouraged(model => model.Selected).SetIsEncouraged(true)),
                nameof(EncouragedModel.Selected), type);
            AssertBinding(ctx, baseline, Serialize(create().BindIsEncouraged(model => model.Selected)
                    .BindIsEncouraged(model => model.OtherSelected).SetIsEncouraged(false)),
                nameof(EncouragedModel.OtherSelected), type);
        }

        private static void AssertBinding(EngineTestContext ctx, JObject baseline, JObject serialized, string binding, string type)
        {
            ctx.Assert(JToken.DeepEquals(new JObject { ["bind"] = binding }, serialized["encouraged"]),
                $"{type}: encouragement must contain the live {binding} reference instead of a static default.");
            AssertOnlyEncouragedChanged(ctx, baseline, serialized, type);
        }

        private static void AssertOnlyEncouragedChanged(EngineTestContext ctx, JObject baseline, JObject serialized, string type)
        {
            var otherFields = (JObject)serialized.DeepClone();
            otherFields.Remove("encouraged");
            ctx.Assert(JToken.DeepEquals(baseline, otherFields), $"{type}: encouragement must preserve every other widget field.");
        }

        private static JObject Serialize(IGuiWidget widget) => JObject.Parse(JsonDump(widget.ToJson()));

        private sealed class EncouragedModel : GuiViewModelBase<EncouragedModel, GuiPayloadBase>
        {
            public bool Selected { get; set; }
            public bool OtherSelected { get; set; }
            public bool Enabled { get; set; }

            protected override void Initialize(GuiPayloadBase initialPayload) { }
        }
    }
}
