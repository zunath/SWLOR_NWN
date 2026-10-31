using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Part browser opened from the Appearance Editor's part picture: view buttons, a grid of
    /// pictures and a pager. The grid is a partial per size, swapped in to fit the window. The
    /// worn part is outlined. Only the pictures scroll.
    /// </summary>
    public class AppearancePartBrowserDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearancePartBrowserViewModel> _builder = new();

        public const float WindowWidth = 620f;
        public const float WindowHeight = 590f;
        private const float TileSize = AppearanceEditorDefinition.TileSize;
        /// <summary>The group that scrolls the pictures; the views and pager sit outside it.</summary>
        public const string PicturesGroupId = "ae_browser_pictures";

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.AppearancePartBrowser)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Part Browser")
                .AddColumn(root => root.AddRow(row => row.AddPartialView(AppearancePartBrowserViewModel.GridElement)));
            for (var columns = AppearancePartBrowserViewModel.MinColumns; columns <= AppearancePartBrowserViewModel.MaxColumns; columns++)
            {
                for (var rows = 1; rows <= AppearancePartBrowserViewModel.MaxRows; rows++)
                {
                    var (c, r) = (columns, rows);
                    window.DefinePartialView(AppearancePartBrowserViewModel.Grid(c, r), partial => BuildGrid(partial, c, r));
                }
            }

            return _builder.Build();
        }

        /// <summary>Builds one grid partial as the window defines it (used by tests).</summary>
        public static GuiGroup<AppearancePartBrowserViewModel> BuildGridPanel(int columns, int rows)
        {
            var panel = new GuiGroup<AppearancePartBrowserViewModel>();
            BuildGrid(panel, columns, rows);
            return panel;
        }

        private static void BuildGrid(GuiGroup<AppearancePartBrowserViewModel> partial, int columns, int rows)
        {
            var narrow = columns <= AppearancePartBrowserViewModel.MinColumns;
            partial.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
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
                            .SetWidth(narrow ? 64f : 80f)
                            .SetHeight(28f)
                            .BindOnClicked(model => model.OnClickView(view));
                    }
                    row.AddSpacer();
                });

                // Only the pictures scroll in a window smaller than the smallest grid: a combo box
                // inside a scrolled group does not open, so the pager stays below it.
                col.AddRow(pictures => pictures.AddGroup(group =>
                {
                    group.SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto);
                    group.AddColumn(grid =>
                    {
                        for (var row = 0; row < rows; row++)
                        {
                            var first = row * columns;
                            grid.AddRow(tiles =>
                            {
                                tiles.AddSpacer();
                                for (var column = 0; column < columns; column++)
                                    AddTile(tiles, first + column);
                                tiles.AddSpacer();
                            });
                        }

                        grid.AddRow(fill => fill.AddSpacer());
                    });
                }).SetId(PicturesGroupId));
                if (narrow)
                {
                    col.AddRow(row =>
                    {
                        AddPageButtons(row, 100f);
                        row.AddSpacer();
                    });
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        AddPartNumber(row);
                    });
                }
                else
                {
                    col.AddRow(row =>
                    {
                        AddPageButtons(row, 140f);
                        row.AddSpacer();
                        AddPartNumber(row);
                    });
                }
            });
        }

        private static void AddTile(GuiRow<AppearancePartBrowserViewModel> tiles, int tile)
        {
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

        private static void AddPageButtons(GuiRow<AppearancePartBrowserViewModel> row, float comboWidth)
        {
            row.AddButton().SetId("ae_browser_previous").SetText("<").SetWidth(30f).SetHeight(30f)
                .BindOnClicked(model => model.OnClickPrevious());
            row.AddComboBox()
                .BindOptions(model => model.PageOptions)
                .BindSelectedIndex(model => model.PageIndex)
                .SetWidth(comboWidth)
                .SetHeight(30f);
            row.AddButton().SetId("ae_browser_next").SetText(">").SetWidth(30f).SetHeight(30f)
                .BindOnClicked(model => model.OnClickNext());
        }

        private static void AddPartNumber(GuiRow<AppearancePartBrowserViewModel> row)
        {
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
        }

        private static Expression<System.Func<AppearancePartBrowserViewModel, TValue>> Bind<TValue>(string propertyName)
        {
            var model = Expression.Parameter(typeof(AppearancePartBrowserViewModel), "model");
            return Expression.Lambda<System.Func<AppearancePartBrowserViewModel, TValue>>(
                Expression.Property(model, propertyName), model);
        }
    }
}
