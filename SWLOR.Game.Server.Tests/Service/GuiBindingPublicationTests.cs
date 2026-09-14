using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class GuiBindingPublicationTests
{
    [Test]
    public void RepublishSendsCurrentOptionsSelectionsAndRegionsWithoutExecutingSetters()
    {
        var options = new GuiBindingList<GuiComboEntry> { new("Robe 187", 187) };
        var region = new GuiRectangle(224, 160, 16, 16);
        var model = new PublicationViewModel
        {
            Options = options,
            Selection = 187,
            Region = region,
            IsVisible = true,
            Unassigned = null
        };
        var setterCalls = model.SelectionSetterCalls;
        var notifications = new List<string>();
        model.PropertyChanged += (_, change) => notifications.Add(change.PropertyName!);

        model.Publish();

        notifications.Should().BeEquivalentTo(new[]
        {
            nameof(model.Options), nameof(model.Selection), nameof(model.Region), nameof(model.IsVisible)
        });
        model.SelectionSetterCalls.Should().Be(setterCalls,
            "replacing controls must not repeat an appearance-changing setter");
        model.Options.Should().BeSameAs(options);
        model.Region.Should().BeSameAs(region);
        model.Selection.Should().Be(187);
        model.IsVisible.Should().BeTrue();
    }

    [Test]
    public void RepublishTakesASnapshotBeforeObserversAddBindings()
    {
        var model = new PublicationViewModel { Selection = 187, IsVisible = true };
        var notifications = new List<string>();
        model.PropertyChanged += (_, change) =>
        {
            notifications.Add(change.PropertyName!);
            if (change.PropertyName == nameof(model.Selection))
                model.Unassigned = "Added by a subscriber";
        };

        var publish = () => model.Publish();
        publish.Should().NotThrow();

        notifications.Should().Equal(nameof(model.Selection), nameof(model.Unassigned), nameof(model.IsVisible));
        model.Unassigned.Should().Be("Added by a subscriber");
    }

    private sealed class PublicationViewModel : GuiViewModelBase<PublicationViewModel, GuiPayloadBase>
    {
        public GuiBindingList<GuiComboEntry> Options
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int Selection
        {
            get => Get<int>();
            set
            {
                SelectionSetterCalls++;
                Set(value);
            }
        }

        public int SelectionSetterCalls { get; private set; }

        public GuiRectangle Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public bool IsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Unassigned
        {
            get => Get<string>();
            set => Set(value);
        }

        public void Publish() => RepublishBindings();

        protected override void Initialize(GuiPayloadBase initialPayload) { }
    }
}
