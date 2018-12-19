using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#pragma warning disable S1206
#pragma warning disable S4035

namespace OrdersApi.Cqrs.Models
{ 
    public class ValueObject<TValueObject> : IEquatable<TValueObject>
        where TValueObject : ValueObject<TValueObject>
    {
        #region IEquatable and Override Equals operators

        /// <summary>
        /// True if an object equals the other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TValueObject other)
        {
            if ((object)other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            //compara todas as propriedades públicas
            var publicProperties = this.GetType().GetProperties();

            if (publicProperties.Any())
            {
                return publicProperties.All(p =>
                {
                    var left = p.GetValue(this, null);
                    var right = p.GetValue(other, null);


                    if (left is TValueObject)
                    {
                        //checa por self-references
                        return ReferenceEquals(left, right);
                    }
                    else
                        return left.Equals(right);
                });
            }
            else
                return true;
        }

        /// <summary>
        /// True if an object equals the other
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            var item = obj as ValueObject<TValueObject>;

            return (object)item != null && Equals((TValueObject)item);
        }

        /// <summary>
        /// Get object hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = 31;
            var changeMultiplier = false;
            var index = 1;

            //compare todas as propriedades públicas
            var publicProperties = this.GetType().GetProperties();


            if (!publicProperties.Any()) return hashCode;

            foreach (var value in publicProperties.Select(item => item.GetValue(this, null)))
            {
                if (value != null)
                {

                    hashCode = hashCode * ((changeMultiplier) ? 59 : 114) + value.GetHashCode();

                    changeMultiplier = !changeMultiplier;
                }
                else
                    hashCode = hashCode ^ (index * 13);
            }

            return hashCode;
        }

        /// <summary>
        /// Operator override
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(ValueObject<TValueObject> left, ValueObject<TValueObject> right)
        {
            return Object.Equals(left, null) ? Object.Equals(right, null) : left.Equals(right);
        }

        /// <summary>
        ///  Operator override
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ValueObject<TValueObject> left, ValueObject<TValueObject> right)
        {
            return !(left == right);
        }

        #endregion
    }
}
