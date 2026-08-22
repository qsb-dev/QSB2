using MessagePack;
using QSB2.Messaging;
using QSB2.QObject;
using SteamTransport;
using UnityEngine;

namespace QSB2.PositionSync
{
    /// <summary>
    /// abstraction over any synced lerped value
    /// </summary>
    public abstract class ValueSync(QObject.QObject qObject)
    {
        protected abstract object SmoothDamp(object from, object to);
        protected abstract void ResetCurrentVelocity();
        protected abstract ValSyncMessage CreateMessage(object value);
        /// <summary>
        /// link to the thing in the world
        /// </summary>
        protected abstract object Link { get; set; }

        private object _lerpedValue, _value;

        public float UpdateInterval = .1f;
        private float _timer;

        public bool Lerp = true;

        public void Tick()
        {
            if (qObject.Owner.ID == -1) return; // no owner = do nothing

            if (qObject.Owner.DoWeOwn)
            {
                _timer += Time.unscaledDeltaTime;
                if (_timer < UpdateInterval) return;
                _timer = 0;

                _lerpedValue = _value = Link;
                ResetCurrentVelocity();

                // owner - sync from unity component
                CreateMessage(_value).Send(SendTo.Others, Channels.Unreliable);
            }
            else
            {
                if (Lerp)
                {
                    _lerpedValue = SmoothDamp(_lerpedValue, _value);
                    Link = _lerpedValue;
                }
            }
        }

        public void OnReceive(object value)
        {
            if (Lerp)
            {
                _value = value;
            }
            else
            {
                _lerpedValue = _value = Link = value;
                ResetCurrentVelocity();
            }
        }
    }

    public abstract class ValSyncMessage : QObjectMessage
    {
        [Key(2)] public required int Index;

        public override void OnReceive(QObject.QObject qObject, int from, int to) => qObject.ValueSyncs[Index].OnReceive(GetValue());

        protected abstract object GetValue();
    }

    namespace Example
    {
        public class FloatValSync(QObject.QObject qObject, int index) : ValueSync(qObject)
        {
            private float _currentVelocity;

            protected override object SmoothDamp(object from, object to) => Mathf.SmoothDamp((float)from, (float)to, ref _currentVelocity, UpdateInterval);
            protected override void ResetCurrentVelocity() => _currentVelocity = 0;
            protected override ValSyncMessage CreateMessage(object value) => new FloatValSyncMessage { Value = (float)value, Index = index };
            protected override object Link { get; set; } // no link for this example
        }

        [MessagePackObject]
        public class FloatValSyncMessage : ValSyncMessage
        {
            [Key(3)] public required float Value;
            protected override object GetValue() => Value;
        }
    }
}