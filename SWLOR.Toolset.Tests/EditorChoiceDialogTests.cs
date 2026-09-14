using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Shell.Views;

namespace SWLOR.Toolset.Tests
{
    public class EditorChoiceDialogTests
    {
        [AvaloniaTest]
        public void ButtonChoiceWaitsForTheCurrentInputTurnBeforeClosing()
        {
            var owner = new Window();
            var dialog = new EditorChoiceDialog();

            try
            {
                owner.Show();
                var result = dialog.ShowDialog<EditorDialogChoice>(owner);
                var click = new RoutedEventArgs(Button.ClickEvent);

                dialog.FindControl<Button>("PrimaryButton")!.RaiseEvent(click);

                click.Handled.Should().BeTrue();
                dialog.IsVisible.Should().BeTrue(
                    "the native window must survive until Avalonia finishes the current input pass");
                result.IsCompleted.Should().BeFalse();

                Dispatcher.UIThread.RunJobs();

                result.IsCompletedSuccessfully.Should().BeTrue();
                result.Result.Should().Be(EditorDialogChoice.Primary);
            }
            finally
            {
                if (dialog.IsVisible)
                    dialog.Close();
                if (owner.IsVisible)
                    owner.Close();
            }
        }
    }
}
