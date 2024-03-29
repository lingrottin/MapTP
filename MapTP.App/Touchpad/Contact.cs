﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapTP.App.Touchpad
{

    public struct TouchpadContact : IEquatable<TouchpadContact>
    {
        public int ContactId { get; }
        public int X { get; }
        public int Y { get; }
        public int TipSwitch {get;}

        public TouchpadContact(int contactId, int x, int y, int tipSwitch) =>
            (this.ContactId, this.X, this.Y, this.TipSwitch) = (contactId, x, y, tipSwitch);

        public override bool Equals(object obj) => (obj is TouchpadContact other) && Equals(other);

        public bool Equals(TouchpadContact other) =>
            (this.ContactId == other.ContactId) && (this.X == other.X) && (this.Y == other.Y) && (this.TipSwitch==other.TipSwitch);

        public static bool operator ==(TouchpadContact a, TouchpadContact b) => a.Equals(b);
        public static bool operator !=(TouchpadContact a, TouchpadContact b) => !(a == b);

        public override int GetHashCode() => (this.ContactId, this.X, this.Y, this.TipSwitch).GetHashCode();

        public override string ToString() => $"Contact ID:{ContactId} Point:{X},{Y} Tip Switch:{TipSwitch}";
    }

    internal class TouchpadContactCreator
    {
        public int? ContactId { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? TipSwitch { get; set; }

        public bool TryCreate(out TouchpadContact contact)
        {
            if (ContactId.HasValue && X.HasValue && Y.HasValue && TipSwitch.HasValue)
            {
                contact = new TouchpadContact(ContactId.Value, X.Value, Y.Value, TipSwitch.Value);
                return true;
            }
            contact = default;
            return false;
        }

        public void Clear()
        {
            ContactId = null;
            X = null;
            Y = null;
        }
    }
}

