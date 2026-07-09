using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class HoloComDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HoloComViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.HoloCom)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 980f, 760f)
                .SetTitle("HoloCom")
                .DefinePartialView(HoloComViewModel.MessagesTabPartial, AddMessagesTab)
                .DefinePartialView(HoloComViewModel.ContactsTabPartial, AddContactsTab)
                .DefinePartialView(HoloComViewModel.ComposePartial, AddComposeTab)
                .AddColumn(root =>
                {
                    root.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.SetShowBorder(false);
                            group.SetScrollbars(NuiScrollbars.Auto);
                            group.AddColumn(tabColumn =>
                            {
                                tabColumn.AddRow(tabRow =>
                                {
                                    tabRow.SetHeight(28f);
                                    tabRow.AddToggles()
                                        .AddOption("Messages")
                                        .AddOption("Contacts")
                                        .BindSelectedValue(model => model.TabToggleValue)
                                        .SetWidth(240f)
                                        .SetHeight(28f);
                                    tabRow.AddSpacer();
                                });
                            });
                        })
                            .SetHeight(40f);
                    });

                    root.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.SetShowBorder(false);
                            group.SetScrollbars(NuiScrollbars.Auto);
                            group.AddColumn(contentCol =>
                            {
                                contentCol.AddRow(contentRow =>
                                {
                                    contentRow.AddPartialView(HoloComViewModel.TabContentPartialElement);
                                });
                            });
                        });
                    });
                });

            return _builder.Build();
        }

        private static void AddMessagesTab(GuiGroup<HoloComViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Refresh")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickRefreshMessages());

                    row.AddToggleButton()
                        .SetText("Unread Only")
                        .SetWidth(120f)
                        .SetHeight(32f)
                        .BindIsToggled(model => model.ShowUnreadOnly);

                    row.AddButton()
                        .SetText("Delete Read")
                        .SetWidth(100f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickDeleteRead());

                    row.AddSpacer();
                });

                // The table lives inside a scrollable group with no fixed height: the
                // group compresses to whatever viewport the tab-content host provides
                // (fixed-height content taller than a scrollbars-None host is unsolvable),
                // and overflowing rows scroll. See Readmes/NuiLayoutRules.md.
                col.AddRow(row =>
                {
                    row.AddGroup(group =>
                    {
                        group.SetShowBorder(false);
                        group.SetScrollbars(NuiScrollbars.Auto);
                        group.AddColumn(tableCol =>
                        {
                            tableCol.AddTable<HoloComViewModel>(t => t
                                .AddComponentColumn("FROM", 180f, cell =>
                                {
                                    cell.AddLabel()
                                        .BindText(model => model.MessageSenderNames)
                                        .BindColor(model => model.MessageRowColors)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                })
                                .AddColumn("RECEIVED", 140f, model => model.MessageTimestamps)
                                .AddColumn("PREVIEW", 0f, model => model.MessagePreviews, isVariable: true)
                                .AddComponentColumn("", 60f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Play")
                                        .SetWidth(60f)
                                        .SetHeight(28f)
                                        .BindOnClicked(model => model.OnClickPlayMessage());
                                })
                                .AddComponentColumn("", 90f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Mark Read")
                                        .SetWidth(90f)
                                        .SetHeight(28f)
                                        .BindOnClicked(model => model.OnClickMarkReadRow())
                                        .BindIsEnabled(model => model.MessageIsUnread);
                                }, isVariable: false)
                                .SetRowHeight(28f));
                        });
                    });
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();

                    row.AddButton()
                        .SetText("<")
                        .SetWidth(32f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickPrevPage())
                        .BindIsEnabled(model => model.IsPrevPageEnabled);

                    row.AddLabel()
                        .BindText(model => model.InboxPageLabel)
                        .SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);

                    row.AddButton()
                        .SetText(">")
                        .SetWidth(32f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickNextPage())
                        .BindIsEnabled(model => model.IsNextPageEnabled);

                    row.AddSpacer();
                });
            });
        }

        private static void AddContactsTab(GuiGroup<HoloComViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.ActiveCallLabel)
                        .SetHeight(24f)
                        .BindIsVisible(model => model.IsInActiveCall);

                    row.AddButton()
                        .SetText("End Call")
                        .SetWidth(120f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickEndCall())
                        .BindIsVisible(model => model.IsInActiveCall);
                });

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.IncomingCallLabel)
                        .SetHeight(24f)
                        .BindIsVisible(model => model.HasIncomingCall);

                    row.AddButton()
                        .SetText("Answer")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickAnswerCall())
                        .BindIsVisible(model => model.HasIncomingCall);

                    row.AddButton()
                        .SetText("Decline")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickDeclineCall())
                        .BindIsVisible(model => model.HasIncomingCall);
                });

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.OutgoingCallLabel)
                        .SetHeight(24f)
                        .BindIsVisible(model => model.HasOutgoingCall);

                    row.AddButton()
                        .SetText("Cancel Call")
                        .SetWidth(120f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickCancelOutgoingCall())
                        .BindIsVisible(model => model.HasOutgoingCall);
                });

                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Refresh")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickRefreshContacts());
                    row.AddSpacer();
                })
                    .BindIsVisible(model => model.IsContactsListVisible);

                col.AddRow(row =>
                {
                    row.AddColumn(onlineCol =>
                    {
                        AddSectionHeader(onlineCol, "Online Players");
                        onlineCol.AddTable<HoloComViewModel>(t => t
                            .AddComponentColumn("", 20f, cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.OnlinePlayerNames)
                                    .BindColor(model => model.OnlinePlayerColors)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            }, isVariable: true)
                            .AddComponentColumn("", 60f, cell =>
                            {
                                cell.AddButton()
                                    .SetText("Call")
                                    .SetWidth(60f)
                                    .SetHeight(26f)
                                    .BindOnClicked(model => model.OnClickCallOnline());
                            })
                            .AddComponentColumn("", 70f, cell =>
                            {
                                cell.AddButton()
                                    .SetText("Message")
                                    .SetWidth(70f)
                                    .SetHeight(26f)
                                    .BindOnClicked(model => model.OnClickMessageOnline());
                            })
                            .AddComponentColumn("", 70f, cell =>
                            {
                                cell.AddButton()
                                    .SetText("Favorite")
                                    .SetWidth(70f)
                                    .SetHeight(26f)
                                    .BindOnClicked(model => model.OnClickFavoriteOnline());
                            }, isVariable: false)
                            .SetShowHeader(false)
                            .SetRowHeight(30f)
                            .BindRowCount(model => model.OnlinePlayerNames));
                    });

                    row.AddColumn(favCol =>
                    {
                        AddSectionHeader(favCol, "Favorites");
                        favCol.AddTable<HoloComViewModel>(t => t
                            .AddComponentColumn("", 20f, cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.FavoriteNames)
                                    .BindColor(model => model.FavoriteStatusColors)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            }, isVariable: true)
                            .AddComponentColumn("", 60f, cell =>
                            {
                                cell.AddButton()
                                    .SetText("Call")
                                    .SetWidth(60f)
                                    .SetHeight(26f)
                                    .BindOnClicked(model => model.OnClickCallFavorite())
                                    .BindIsEnabled(model => model.FavoriteIsOnline);
                            })
                            .AddComponentColumn("", 70f, cell =>
                            {
                                cell.AddButton()
                                    .SetText("Message")
                                    .SetWidth(70f)
                                    .SetHeight(26f)
                                    .BindOnClicked(model => model.OnClickMessageFavorite());
                            })
                            .AddComponentColumn("", 70f, cell =>
                            {
                                cell.AddButton()
                                    .SetText("Remove")
                                    .SetWidth(70f)
                                    .SetHeight(26f)
                                    .BindOnClicked(model => model.OnClickRemoveFavorite());
                            }, isVariable: false)
                            .SetShowHeader(false)
                            .SetRowHeight(30f)
                            .BindRowCount(model => model.FavoriteNames));
                    })
                        .SetWidth(480f);
                })
                    .BindIsVisible(model => model.IsContactsListVisible);
            });
        }

        private static void AddComposeTab(GuiGroup<HoloComViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Back")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickComposeBack());

                    row.AddLabel()
                        .BindText(model => model.ComposeRecipientLabel)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                // Scrollable, unsized wrapper so the fixed-height editor can never make the
                // partial's required height exceed the host viewport (which is unsolvable).
                col.AddRow(row =>
                {
                    row.AddGroup(group =>
                    {
                        group.SetShowBorder(false);
                        group.SetScrollbars(NuiScrollbars.Auto);
                        group.AddColumn(editorCol =>
                        {
                            editorCol.AddRow(editorRow =>
                            {
                                editorRow.AddTextEdit()
                                    .SetIsMultiline(true)
                                    .SetMaxLength(HoloComMessaging.MaxMessageLength)
                                    .BindValue(model => model.ComposeText)
                                    .SetPlaceholder("Type message, then choose Send below...")
                                    .SetHeight(450f);
                            });
                        });
                    });
                });

                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Send")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickSend());

                    row.AddButton()
                        .SetText("Clear")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickClearCompose());

                    row.AddSpacer();
                });
            });
        }

        private static void AddSectionHeader(GuiColumn<HoloComViewModel> col, string text)
        {
            col.AddRow(row =>
                row.AddLabel()
                    .SetText(text)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

    }
}
