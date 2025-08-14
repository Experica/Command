module Experica
{
    interface Command
    {
        object getEnvParam(string name);
        bool setEnvParam(string name, object value);

        EXPERIMENTSTATUS getExperimentStatus();
        void setExperimentStatus(EXPERIMENTSTATUS status);

        Experiment getEx()
        void setEx(Experiment ex)

        object getExParam(string name);
        bool setExParam(string name, object value);

        Dictionary<string,object> getCondTest();
        void setCondTest(Dictionary<string,object> condtest)
    }
}