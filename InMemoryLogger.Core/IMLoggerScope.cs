using System;
using System.Threading;

namespace InMemoryLogger.Core
{
    public class IMLoggerScope
    {
        private readonly string _name;
        private readonly object _state;

        public IMLoggerScope(string name, object state)
        {
            _name = name;
            _state = state;
        }

        public ActivityContext Context { get; set; }

        public IMLoggerScope Parent { get; set; }

        public IMLoggerScopeNode Node { get; set; }

        private static AsyncLocal<IMLoggerScope> _value = new AsyncLocal<IMLoggerScope>();

        public static IMLoggerScope Current
        {
            set
            {
                _value.Value = value;
            }
            get
            {
                return _value.Value;
            }
        }

        public static IDisposable Push(IMLoggerScope scope, IMLoggerStore store)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var temp = Current;
            Current = scope;
            Current.Parent = temp;

            Current.Node = new IMLoggerScopeNode()
            {
                StartTime = DateTimeOffset.UtcNow,
                State = Current._state,
                Name = Current._name
            };

            if (Current.Parent != null)
            {
                Current.Node.Parent = Current.Parent.Node;
                Current.Parent.Node.Children.Add(Current.Node);
            }
            else
            {
                Current.Context.Root = Current.Node;
                store.AddActivity(Current.Context);
            }

            return new DisposableAction(() =>
            {
                Current.Node.EndTime = DateTimeOffset.UtcNow;
                Current = Current.Parent;
            });
        }

        public class DisposableAction : IDisposable
        {
            private Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                if (_action != null)
                {
                    _action.Invoke();
                    _action = null;
                }
            }
        }
    }
}
