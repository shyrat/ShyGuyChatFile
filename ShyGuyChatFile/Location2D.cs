using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile
{
    public struct Location2D : IEquatable<Location2D>
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public Location2D(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Location2D))
                return false;

            return Equals((Location2D)obj);
        }

        public override int GetHashCode()
        {
            return Latitude.GetHashCode() ^ Longitude.GetHashCode();
        }

        public bool Equals(Location2D other)
        {
            return other.Latitude == this.Latitude && other.Longitude == this.Longitude;
        }

        public static bool operator ==(Location2D a, Location2D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Location2D a, Location2D b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"{Latitude},{Longitude}";
        }
    }

}
