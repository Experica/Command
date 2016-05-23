// Real-Time Task Control requires checking States constantly, often in a loop.

// Foraging Visual Search Task
class VisualForaging
{
    int TaskState;
    double PreITIOnTime, FixOnTime, FixTargetOnTime, SufITIOnTime;
    double FigArrayOnTime, FigFixOnTime;


    VisualForaging()
    {
        // Initial State of Task
        EnterTaskState(PREITI);
    }

    double PreITIHold()
    {
        return CurrentTime() - PreITIOnTime;
    }
    double FixHold()
    {
        return CurrentTime() - FixOnTime;
    }
    double WaitForFix()
    {
        return CurrentTime() - FixTargetOnTime;
    }
    double FigArrayHold()
    {
        return CurrentTime() - FigArrayOnTime;
    }
    double FigFixHold()
    {
        return CurrentTime() - FigFixOnTime;
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
            case FIXTARGET_ON:
                TurnOnFixTarget();
                FixTargetOnTime = CurrentTime();
            case FIX_ACQUIRED:
                FixDur = RandFixDur();
                FixOnTime = CurrentTime();
            case SUFITI:
                SufITIDur = RandSufITIDur();
                SufITIOnTime = CurrentTime();
            case FIGARRAY_ON:
                FigArrayDur = RandFigArrayDur();
                TurnOnFigArray();
                FigArrayOnTime = CurrentTime();
            case FIGFIX_ACQUIRED:
                FigFixDur = RandFigFixDur();
                FigFixOnTime = CurrentTime();
            case FIGFIX_LOST:
        }
        TaskState = nexttaskstate;
    }


    // State Machine Updated in a loop
    int TaskControl()
    {
        switch (TaskState)
        {
            case PREITI:
                // Turn on Fixation Target when PREITI expired.
                if (PreITIHold() >= PreITIDur)
                {
                    EnterTaskState(FIXTARGET_ON);
                }
            case FIXTARGET_ON:
                if (FixOnTarget())
                {
                    EnterTaskState(FIX_ACQUIRED);
                }
                else if (WaitForFix() >= WaitForFixTimeOut)
                {
                    // Failed to acquire fixation
                    TurnOffFixTarget();
                    Punish();
                    EnterTaskState(PREITI);
                    return TASKTRIAL_FAIL;
                }
            case FIX_ACQUIRED:
                if (!FixOnTarget())
                {
                    // Fixation breaks in required period.
                    TurnOffFixTarget();
                    Punish();
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_EARLY;
                }
                else if (FixHold() >= FixDur)
                {
                    // Successfully hold fixation in required period.
                    TurnOffFixTarget();
                    EnterTaskState(FIGARRAY_ON);
                }
            case FIGARRAY_ON:
                if (FigArrayHold() >= FigArrayDur)
                {
                    TurnOffFigArray();
                    Punish();
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_MISS;
                }
                else if (FixOnFig())
                {
                    EnterTaskState(FIGFIX_ACQUIRED);
                }
            case FIGFIX_ACQUIRED:
                if (!FixOnFig())
                {
                    EnterTaskState(FIGFIX_LOST);
                }
                else if (FigFixHold() >= FigFixDur)
                {
                    if (IsRewardFig())
                    {
                        TurnOffFigArray();
                        Reward();
                        EnterTaskState(SUFITI);
                        return TASKTRIAL_HIT;
                    }
                    else if (IsPunishFig())
                    {
                        TurnOffFigArray();
                        Punish();
                        EnterTaskState(SUFITI);
                        return TASKTRIAL_MISS;
                    }
                    else
                    {
                    }
                }
            case FIGFIX_LOST:
                if (FigArrayHold() >= FigArrayDur)
                {
                    TurnOffFigArray();
                    EnterTaskState(PREITI);
                    return TASKTRIAL_END;
                }
                else if (FixOnFig())
                {
                    EnterTaskState(FIGFIX_ACQUIRED);
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