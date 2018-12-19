using System;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable S2328
#pragma warning disable S3875 
#pragma warning disable S3249

namespace OrdersApi.Cqrs.Models
{
    public abstract class Entity
    {
        #region Members

        private int? _requestedHashCode;

        #endregion

        #region Properties

        public virtual string AggregateKey { get; set; }

        #endregion

        #region Public Methods

        [SuppressMessage("ReSharper", "BaseObjectGetHashCodeCallInGetHashCode")]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            if (IsTransient()) return base.GetHashCode();

            if (!_requestedHashCode.HasValue)
                _requestedHashCode = this.AggregateKey.GetHashCode() ^ 31;  

            return _requestedHashCode.Value;
        }

        public long GetInt64HashCode()
        {
           return IdentityGenerator.GetInt64HashCode(AggregateKey);
        }

        public long GetAbsoluteInt64HashCode()
        {
            return IdentityGenerator.GetAbsoluteInt64HashCode(AggregateKey);
        }

        public virtual bool IsTransient()
        {
            return string.IsNullOrWhiteSpace(AggregateKey);
        }

        public virtual void GenerateNewIdentity()
        {
            if (IsTransient())
            {
                this.AggregateKey = IdentityGenerator.NewSequentialIdentity();
            }


        }

        public void ChangeCurrentIdentity(string identity)
        {
            if (!string.IsNullOrWhiteSpace(identity))
                this.AggregateKey = identity;
        }

        #endregion

        #region Overrides Methods

        public override string ToString()
        {
            return GetType().Name + " [Id=" + AggregateKey + "]";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Entity))
                return false;

            if (object.ReferenceEquals(this, obj))
                return true;

            var item = (Entity)obj;

            if (item.IsTransient() || this.IsTransient())
                return false;

            return item.AggregateKey == this.AggregateKey;
        }

        public static bool operator ==(Entity left, Entity right)
        {
            if (object.Equals(left, null))
                return object.Equals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        #endregion 
    }
}
