namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public static class BackgroundJobQueueNames
    {
        public const string StreamName = "swlor:background-jobs";
        public const string DeadLetterStreamName = "swlor:background-jobs:dead";
        public const string ConsumerGroup = "swlor-background-services";
        public const int MaxStreamLength = 10000;
    }
}
