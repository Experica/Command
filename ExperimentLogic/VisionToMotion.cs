// Real-Time Task Control requires checking States constantly, often in a loop.

// Basic Visual Task Training
// PreStage: Familiar with Periodic Reward
// Stage1: Learn to Associate Axis Movement to Reward
// Stage2: Learn to Associate Reward to Visual Stimulus Presentation
// Stage3: Learn to Fix and Attend to Visual Stimulus Change
// SufStage: Decrease Visual Stimulus Size and Maintain High Performance
class VisualBasic
{
    int TaskState, Stage;
    double PreITIOnTime, AxisForceOnTime, TargetOnTime, TargetChangeOnTime, SufITIOnTime;


    VisualBasic()
    {
        // Initial State of Task and Stage.
        Stage = 1;
        EnterTaskState(PREITI);
    }

    double PreITIHold()
    {
        return CurrentTime() - PreITIOnTime;
    }
    double AxisForceHold()
    {
        return CurrentTime() - AxisForceOnTime;
    }
    double WaitForAxisForce()
    {
        return CurrentTime() - TargetOnTime;
    }
    double ReactionTime()
    {
        return CurrentTime() - TargetChangeOnTime;
    }
    double SufITIHold()
    {
        return CurrentTime() - SufITIOnTime;
    }

    void EnterTaskState(int nexttaskstate)
    {
        // Action when enter next task state
        switch (nexttaskstate)
        {
            case PREITI:
                PreITIDur = RandPreITIDur();
                PreITIOnTime = CurrentTime();
            case TARGET_ON:
                TurnOnTarget();
                TargetOnTime = CurrentTime();
            case AXISFORCED:
                AxisForceHoldDur = RandForceHoldDur();
                AxisForceOnTime = CurrentTime();
            case TARGET_CHANGE:
                ChangeTarget();
                TargetChangeOnTime = CurrentTime();
            case REACTIONALLOWED:
            case SUFITI:
                SufITIDur = RandSufITIDur();
                SufITIOnTime = CurrentTime();
        }
        TaskState = nexttaskstate;
    }


    // State Machine Updated in a loop
    int TaskControl()
    {
        switch (TaskState)
        {
            case PREITI:
                if (!AxisZero())
                {
                    switch (Stage)
                    {
                        case 1:
                            EnterTaskState(AXISFORCED);
                        case 2:
                        case 3:
                            EnterTaskState(PREITI);
                    }
                }
                else if (PreITIHold() >= PreITIDur)
                {
                    // Turn on Visual Target when PREITI expired.
                    EnterTaskState(TARGET_ON);
                }
            case TARGET_ON:
                if (!AxisZero())
                {
                    EnterTaskState(AXISFORCED);
                }
                else if (WaitForAxisForce() >= WaitForAxisForceTimeOut)
                {
                    // Failed to Force Joystick Axis
                    TurnOffTarget();
                    switch (Stage)
                    {
                        case 1:
                        case 2:
                        case 3:
                            Punish();
                    }
                    EnterTaskState(PREITI);
                    return TASKTRIAL_FAIL;
                }
            case AXISFORCED:
                if (AxisZero())
                {
                    // Joystick Lose Force and Return to Zero in required period.
                    TurnOffTarget();
                    switch (Stage)
                    {
                        case 1:
                            Reward();
                        case 2:
                            Reward();
                        case 3:
                            Punish();
                    }
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_EARLY_HOLD;
                }
                else if (AxisForceHold() >= AxisForceHoldDur)
                {
                    EnterTaskState(TARGET_CHANGE);
                }
            case TARGET_CHANGE:
                if (AxisZero())
                {
                    // Joystick Release Early
                    TurnOffTarget();
                    switch (Stage)
                    {
                        case 1:
                            Reward();
                        case 2:
                            Reward();
                        case 3:
                    }
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_EARLY_RELEASE;
                }
                else if (ReactionTime() >= MinReleaseDur)
                {
                    EnterTaskState(REACTIONALLOWED);
                }
            case REACTIONALLOWED:
                if (AxisZero())
                {
                    // Joystick Released in Release period.
                    TurnOffTarget();
                    Reward();
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_HIT;
                }
                else if (ReactionTime() >= MaxReleaseDur)
                {
                    // Joystick Not Released in Release period.
                    TurnOffTarget();
                    switch (Stage)
                    {
                        case 1:
                        case 2:
                        case 3:
                            Punish();
                    }
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_MISS;
                }
            case SUFITI:
                if (SufITIHold() >= SufITIDur)
                {
                    EnterTaskState(PREITI);
                }
        }
        return TASKTRIAL_CONTINUE;
    }
}