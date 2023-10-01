namespace InterviewCopilotServer.Helpers
{
    public class RequestThresholdPerDay
    {
        private int threshold;
        private int requestCount = 0;
        private int currentDayOfYear;

        public RequestThresholdPerDay(int threshold) 
        {
            this.threshold = threshold;
            this.currentDayOfYear = DateTimeOffset.Now.DayOfYear;
        }
        
        public bool AllowRequest()
        {
            int dayOfYear = DateTimeOffset.Now.DayOfYear;
            if (dayOfYear != this.currentDayOfYear)
            {
                this.requestCount = 0;
                this.currentDayOfYear = dayOfYear;
                return true;
            }
            requestCount++;
            return requestCount < threshold;
        }

    }

}
