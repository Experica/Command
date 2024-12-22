module Experica
{
    interface Agent
    {
        bool getEnvBool(string name);
        bool setEnvBool(string name, bool value);
        float getEnvFloat(string name);
        bool setEnvFloat(string name, float value);
    }
}