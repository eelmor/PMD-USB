namespace PMD
{
    public class Sensor
    {
        public delegate void SensorUpdatedEventHandler(double value);
        public event SensorUpdatedEventHandler SensorUpdated;

        public string DescriptionLong { get; }
        public string DescriptionShort { get; }
        public string Format { get; }
        public string Unit { get; }

        private double _value;
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                SensorUpdated?.Invoke(_value);
            }
        }

        public Sensor(string description_long, string description_short, string format, string unit)
        {
            DescriptionLong = description_long;
            DescriptionShort = description_short;
            Format = format;
            Unit = unit;
        }
    }
}