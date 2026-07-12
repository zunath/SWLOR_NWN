using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the boot-time NUI layout safety checks: the GuiTableBuilder fixed-zero-width
/// column guard (hard throw) and the GuiLayoutValidator advisory findings. All checks
/// run on the widget tree before Json serialization, so no NWN engine is required.
/// See SWLOR.Game.Server/Readmes/NuiLayoutRules.md for the rules under test.
/// </summary>
public class GuiLayoutValidationTests
{
    [Test]
    public void GuiTable_FixedColumnWithoutWidth_ThrowsAtBuildTime()
    {
        var col = new GuiColumn<LayoutTestViewModel>();

        var act = () => col.AddTable<LayoutTestViewModel>(t => t
            // Width 0 and not the last column and no isVariable => resolves to fixed zero-width.
            .AddColumn("BROKEN", 0f, model => model.MessageSenderNames)
            .AddColumn("TRAILING", 100f, model => model.MessageTimestamps));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BROKEN*fixed width*");
    }

    [Test]
    public void GuiTable_VariableColumnWithoutWidth_Builds()
    {
        var col = new GuiColumn<LayoutTestViewModel>();

        var act = () => col.AddTable<LayoutTestViewModel>(t => t
            .AddColumn("FLEX", 0f, model => model.MessageSenderNames, isVariable: true)
            .AddColumn("FIXED", 100f, model => model.MessageTimestamps, isVariable: false));

        act.Should().NotThrow();
    }

    [Test]
    public void Validator_UnboundedListWithTrailingRow_InNamedPartial_IsNotFlagged()
    {
        // W1 (NuiLayoutRules.md, "Verified-working shapes"): an unbounded non-terminal
        // list in a named partial is confirmed to render in-game, so the validator
        // deliberately does NOT warn on it. Guards against reintroducing that false
        // positive.
        var partial = BuildPartialWithListThenRow(boundedListRow: false);

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_UnboundedListWithTrailingRow_InMainPartial_IsNotFlagged()
    {
        var partial = BuildPartialWithListThenRow(boundedListRow: false);

        var findings = GuiLayoutValidator.Validate("test_window", Partials("%%WINDOW_MAIN%%", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_HeightBoundedListWithTrailingRow_IsNotFlagged()
    {
        var partial = BuildPartialWithListThenRow(boundedListRow: true);

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_ListInsideScrollableGroup_WithTrailingRow_IsNotFlagged()
    {
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddGroup(group =>
                {
                    group.SetShowBorder(false);
                    group.SetScrollbars(SWLOR.Game.Server.Core.Beamdog.NuiScrollbars.Auto);
                    group.AddColumn(inner =>
                    {
                        inner.AddRow(innerRow =>
                        {
                            innerRow.AddList(template =>
                            {
                                template.AddCell(cell => cell.AddLabel().BindText(model => model.MessageSenderNames));
                            })
                                .BindRowCount(model => model.MessageSenderNames);
                        });
                    });
                });
            });

            col.AddRow(row =>
            {
                row.AddButton().SetText(">").SetWidth(32f).SetHeight(32f);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_FixedRowWithEqualHeightButton_IsFlagged()
    {
        // A confirmed real-world failure shape: a row with an explicit height whose
        // buttons are the same height leaves no room for default widget margins.
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.SetHeight(32f);
                row.AddButton().SetText("Refresh").SetHeight(32f).SetWidth(90f);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().ContainSingle(f => f.Contains("default margins"));
    }

    [Test]
    public void Validator_TerminalList_IsNotFlagged()
    {
        // The CharacterSheet tab shape: an action row (deriving its height from its
        // children) first, list as the last row.
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddButton().SetText("Refresh").SetHeight(32f).SetWidth(90f);
            });

            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel().BindText(model => model.MessageSenderNames));
                })
                    .BindRowCount(model => model.MessageSenderNames);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_FixedTemplateCellWithoutWidth_IsNotFlagged()
    {
        // W3 (NuiLayoutRules.md, "Verified-working shapes"): a fixed list template cell
        // with no width anywhere is confirmed to render in-game, so the validator
        // deliberately does NOT warn on it. Guards against reintroducing that false
        // positive.
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.AddLabel().BindText(model => model.MessageSenderNames);
                    });
                })
                    .BindRowCount(model => model.MessageSenderNames);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    /// <summary>
    /// Creates a partial wrapper group the way DefinePartialView does: no border,
    /// no scrollbars. A default GuiGroup is Scrollbars.Auto, which the validator
    /// legitimately treats as bounding its content.
    /// </summary>
    private static GuiGroup<LayoutTestViewModel> CreatePartialWrapper()
    {
        var partial = new GuiGroup<LayoutTestViewModel>();
        partial.SetShowBorder(false);
        partial.SetScrollbars(SWLOR.Game.Server.Core.Beamdog.NuiScrollbars.None);
        return partial;
    }

    private static GuiGroup<LayoutTestViewModel> BuildPartialWithListThenRow(bool boundedListRow)
    {
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                if (boundedListRow)
                    row.SetHeight(500f);

                row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel().BindText(model => model.MessageSenderNames));
                })
                    .BindRowCount(model => model.MessageSenderNames);
            });

            // Trailing pagination-style row after the list (height derived from children).
            col.AddRow(row =>
            {
                row.AddButton().SetText(">").SetWidth(32f).SetHeight(32f);
            });
        });

        return partial;
    }

    private static Dictionary<string, IGuiWidget> Partials(string name, IGuiWidget partial)
    {
        return new Dictionary<string, IGuiWidget>
        {
            [name] = partial
        };
    }

    /// <summary>
    /// Minimal view model used only to give the layout builders a data model with a
    /// couple of bindable list properties. The layout validator operates on the widget
    /// tree, so the specific view model is irrelevant to what is under test.
    /// </summary>
    private sealed class LayoutTestViewModel : GuiViewModelBase<LayoutTestViewModel, GuiPayloadBase>
    {
        public GuiBindingList<string> MessageSenderNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> MessageTimestamps
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
        }
    }
}
