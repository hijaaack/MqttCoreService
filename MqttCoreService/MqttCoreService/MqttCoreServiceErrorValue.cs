namespace MqttCoreService
{
    // Contains extension specific error values.
    public static class MqttCoreServiceErrorValue
    {
        public static readonly uint Success = 0;
        public static readonly uint Fail = 1;

        public static readonly uint DataWrongTypeOrEmpty = 10;
        public static readonly uint PublishFailed = 11;

    }
}
