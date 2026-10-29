using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Part browser opened from the Appearance Editor's part picture: view buttons, five by three
    /// pictures and a pager. The worn part is outlined.
    /// </summary>
    public class AppearancePartBrowserDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearancePartBrowserViewModel> _builder = new();

        public const float WindowWidth = 620f;
        public const float WindowHeight = 590f;
        private const float TileSize = AppearanceEditorDefinition.TileSize;
        private const float ViewButtonWidth = 80f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AppearancePartBrowser)
                .SetIsResizable(false)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Part Browser")
                .AddColumn(root => root.AddRow(row => row.AddGroup(group =>
                {
                    // A screen too small for the window scrolls its content instead of cutting it off.
                    group.SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto);
                    group.AddColumn(BuildBrowser);
                })));

            return _builder.Build();
        }

        /// <summary>Builds the browser's column as the window defines it (used by tests).</summary>
        public static GuiColumn<AppearancePartBrowserViewModel> BuildBrowserColumn()
        {
            var col = new GuiColumn<AppearancePartBrowserViewModel>();
            BuildBrowser(col);
            return col;
        }

        private static void BuildBrowser(GuiColumn<AppearancePartBrowserViewModel> col)
        {
            col.AddRow(row =>
            {
                foreach (var (text, view, bind) in new[]
                         {
                             ("Front", AppearancePreviewView.Front, nameof(AppearancePartBrowserViewModel.IsFrontViewSelected)),
                             ("Back", AppearancePreviewView.Back, nameof(AppearancePartBrowserViewModel.IsBackViewSelected)),
                             ("Angle", AppearancePreviewView.Angle, nameof(AppearancePartBrowserViewModel.IsAngleViewSelected))
                         })
                {
                    row.AddToggleButton()
                        .SetId($"ae_view_{view}")
                        .SetText(text)
                        .BindIsToggled(Bind<bool>(bind))
                        .BindIsVisible(model => model.IsPreviewViewChoiceVisible)
                        .SetWidth(ViewButtonWidth)
                        .SetHeight(28f)
                        .BindOnClicked(model => model.OnClickView(view));
                }
                row.AddSpacer();
            });

            for (var row = 0; row < AppearancePartBrowserViewModel.Rows; row++)
            {
                var first = row * AppearancePartBrowserViewModel.Columns;
                col.AddRow(tiles =>
                {
                    tiles.AddSpacer();
                    for (var column = 0; column < AppearancePartBrowserViewModel.Columns; column++)
                    {
                        var tile = first + column;
                        tiles.AddColumn(cell =>
                        {
                            cell.AddRow(imageRow =>
                            {
                                var selected = Bind<bool>($"Tile{tile}Selected");
                                imageRow.AddButtonImage()
                                    .SetId($"ae_tile_{tile}")
                                    .BindImageResref(Bind<string>($"Tile{tile}Image"))
                                    .BindIsVisible(Bind<bool>($"Tile{tile}Visible"))
                                    .BindIsEncouraged(selected)
                                    .SetWidth(TileSize)
                                    .SetHeight(TileSize)
                                    .SetMargin(2f)
                                    .BindOnClicked(model => model.OnClickTile(tile))
                                    .AddDrawList(list => list.AddPolyLine(outline => outline
                                        .AddPoint(1.5f, 1.5f)
                                        .AddPoint(TileSize - 1.5f, 1.5f)
                                        .AddPoint(TileSize - 1.5f, TileSize - 1.5f)
                                        .AddPoint(1.5f, TileSize - 1.5f)
                                        .AddPoint(1.5f, 1.5f)
                                        .BindIsEnabled(selected)
                                        .SetColor(AppearanceEditorDefinition.Highlight)
                                        .SetLineThickness(3f)));
                            });
                            cell.AddRow(labelRow =>
                            {
                                labelRow.AddLabel()
                                    .BindText(Bind<string>($"Tile{tile}Label"))
                                    .BindIsVisible(Bind<bool>($"Tile{tile}Visible"))
                                    .SetWidth(TileSize)
                                    .SetHeight(18f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
                            });
                        }).SetWidth(TileSize + 10f);
                    }
                    tiles.AddSpacer();
                });
            }

            col.AddRow(fill => fill.AddSpacer());
            col.AddRow(row =>
            {
                row.AddButton().SetId("ae_browser_previous").SetText("<").SetWidth(30f).SetHeight(30f)
                    .BindOnClicked(model => model.OnClickPrevious());
                row.AddComboBox()
                    .BindOptions(model => model.PageOptions)
                    .BindSelectedIndex(model => model.PageIndex)
                    .SetWidth(140f)
                    .SetHeight(30f);
                row.AddButton().SetId("ae_browser_next").SetText(">").SetWidth(30f).SetHeight(30f)
                    .BindOnClicked(model => model.OnClickNext());
                row.AddSpacer();
                row.AddTextEdit()
                    .SetId("ae_browser_part_number")
                    .SetPlaceholder("Part #")
                    .SetMaxLength(5)
                    .BindValue(model => model.PartNumberText)
                    .SetWidth(56f)
                    .SetHeight(30f);
                row.AddButton().SetId("ae_browser_go").SetText("Go").SetWidth(40f).SetHeight(30f)
                    .SetTooltip("Wears the part with the number you typed.")
                    .BindOnClicked(model => model.OnClickGo());
            });
        }

        private static Expression<System.Func<AppearancePartBrowserViewModel, TValue>> Bind<TValue>(string propertyName)
        {
            var model = Expression.Parameter(typeof(AppearancePartBrowserViewModel), "model");
            return Expression.Lambda<System.Func<AppearancePartBrowserViewModel, TValue>>(
                Expression.Property(model, propertyName), model);
        }
    }
}
