using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>
    /// Extensible operation and token registry used by the conversation graph. Snippets will be
    /// registered here during migration instead of being reduced to a fixed action enum.
    /// </summary>
    public sealed class ConversationRuntime : IConversationRuntime
    {
        private static readonly Regex TokenPattern = new(
            @"\{\{(?<key>[A-Za-z0-9_.-]+)\}\}",
            RegexOptions.Compiled);

        private readonly Dictionary<string, Func<ConversationContext, IReadOnlyList<string>, bool>> _conditions = new();
        private readonly Dictionary<string, Func<ConversationContext, IReadOnlyList<string>, string, bool>> _actions = new();
        private readonly Dictionary<string, Func<ConversationContext, string>> _tokens = new();

        public void RegisterCondition(
            string key,
            Func<ConversationContext, IReadOnlyList<string>, bool> handler)
        {
            ValidateRegistration(key, handler);
            _conditions.Add(key, handler);
        }

        public void RegisterAction(
            string key,
            Func<ConversationContext, IReadOnlyList<string>, string, bool> handler)
        {
            ValidateRegistration(key, handler);
            _actions.Add(key, handler);
        }

        public void RegisterToken(string key, Func<ConversationContext, string> resolver)
        {
            ValidateRegistration(key, resolver);
            _tokens.Add(key, resolver);
        }

        public bool HasCondition(string key) =>
            !string.IsNullOrWhiteSpace(key) && _conditions.ContainsKey(key);

        public bool HasAction(string key) =>
            !string.IsNullOrWhiteSpace(key) && _actions.ContainsKey(key);

        public bool HasToken(string key) =>
            !string.IsNullOrWhiteSpace(key) && _tokens.ContainsKey(key);

        public bool EvaluateCondition(ConversationContext context, ConversationCondition condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (!_conditions.TryGetValue(condition.Key, out var handler))
                throw new InvalidOperationException($"Conversation condition '{condition.Key}' is not registered.");

            var result = handler(context, condition.Arguments);
            return condition.IsNegated ? !result : result;
        }

        public bool ExecuteAction(ConversationContext context, ConversationAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (!_actions.TryGetValue(action.Key, out var handler))
                throw new InvalidOperationException($"Conversation action '{action.Key}' is not registered.");

            return handler(context, action.Arguments, action.OncePerPlayerId);
        }

        public string ResolveText(ConversationContext context, string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return TokenPattern.Replace(text, match =>
            {
                var key = match.Groups["key"].Value;

                if (context.Tokens.TryGetValue(key, out var value))
                    return value ?? string.Empty;

                return _tokens.TryGetValue(key, out var resolver)
                    ? resolver(context) ?? string.Empty
                    : $"[Unknown token: {key}]";
            });
        }

        private static void ValidateRegistration(string key, Delegate handler)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A conversation registry key is required.", nameof(key));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
        }
    }
}
