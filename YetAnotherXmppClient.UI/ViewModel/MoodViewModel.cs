using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class MoodViewModel
    {
        private const string DisableMoodItemString = "-Disable Mood-";

        public IEnumerable<string> MoodStringValues => DisableMoodItemString.Concat(Enum.GetNames(typeof(Mood)));

        public string SelectedMoodStringValue { get; set; } = DisableMoodItemString;

        public Mood? SelectedMood => Enum.TryParse<Mood>(this.SelectedMoodStringValue, out var mood) ? mood : (Mood?)null;

        public string Text { get; set; }

        public Action<Mood?, string> SubmitAction { get; set; }

        public ReactiveCommand<Unit, Unit> SubmitCommand { get; }

        public MoodViewModel()
        {
            this.SubmitCommand = ReactiveCommand.Create(() => this.SubmitAction(this.SelectedMood, this.Text));
        }
    }

    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Concat<T>(this T item, IEnumerable<T> second) => new T[] { item }.Concat(second);
    }
}
