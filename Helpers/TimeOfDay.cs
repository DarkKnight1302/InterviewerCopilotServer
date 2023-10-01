namespace InterviewCopilotServer.Helpers
{
    [Serializable]
    public struct TimeOfDay : IEquatable<TimeOfDay>
    {
        private int hours;
        private int mins;

        public TimeOfDay(int hours, int mins)
        {
            this.hours = hours;
            this.mins = mins;
        }

        public int Hours => this.hours;

        public int Minutes => this.mins;

        public new string ToString()
        {
            return hours+ ":" + mins;
        }

        public static TimeOfDay Parse(string s)
        {
            var split = s.Split(':');
            int hours = int.Parse(split[0]);
            int mins = int.Parse(split[1]);
            return new TimeOfDay(hours, mins);
        }

        public static bool operator ==(TimeOfDay left, TimeOfDay right)
        {
            return left.hours == right.hours && left.mins == right.mins;
        }
        public static bool operator >(TimeOfDay left, TimeOfDay right)
        {
            if (left.hours > right.hours)
            {
                return true;
            }
            if (left.hours < right.hours)
            {
                return false;
            }
            if (left.mins > right.mins)
            {
                return true;
            }
            return false;
        }

        public static bool operator >=(TimeOfDay left, TimeOfDay right)
        {
            if (left.hours > right.hours)
            {
                return true;
            }
            if (left.hours < right.hours)
            {
                return false;
            }
            if (left.mins >= right.mins)
            {
                return true;
            }
            return false;
        }

        public static bool operator <=(TimeOfDay left, TimeOfDay right)
        {
            if (left.hours > right.hours)
            {
                return false;
            }
            if (left.hours < right.hours)
            {
                return true;
            }
            if (left.mins <= right.mins)
            {
                return true;
            }
            return false;
        }

        public static bool operator <(TimeOfDay left, TimeOfDay right)
        {
            if (left.hours > right.hours)
            {
                return false;
            }
            if (left.hours < right.hours)
            {
                return true;
            }
            if (left.mins < right.mins)
            {
                return true;
            }
            return false;
        }

        public static bool operator !=(TimeOfDay left, TimeOfDay right)
        {
            return left.hours != right.Hours || left.mins != right.mins;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is TimeOfDay)
            {
                return this.hours== ((TimeOfDay)obj).hours && this.mins == ((TimeOfDay)obj).mins;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.hours.GetHashCode() + this.mins.GetHashCode();
        }

        public bool Equals(TimeOfDay other)
        {
            return this.hours == (other).hours && this.mins == (other).mins;
        }
    }
}
