
Purpose:
Send a command or query to running SpikeGLX from external program.

Usage:
>HelloSGLX -cmd=command [ options ]

Options:
-host=127.0.0.1   ;ip4 SpikeGLX network address
-port=4142        ;ip4 port
-args=as_needed   ;optional command arguments

Notes:
- Separate multiple input args by '\n' chars.
- Output is sent to stdout stream.
- Multiple output values are '\n' separated lines.

Commands:
-cmd=justConnect
Returns line: version.

-cmd=enumDataDir [-args=idir] (default 0)
Retrieve a listing of files in idir data directory.
Get main data directory by setting idir=0 or omitting it.

-cmd=getDataDir [-args=idir] (default 0)
Get ith global data directory.
Get main data directory by setting idir=0 or omitting it.

-cmd=getGeomMap [-args=ip] (default 0)
Get imec parameters for given logical probe.
Returns lines: key=value.
Header fields:
  head_partNumber
  head_numShanks
  head_shankPitch   ; microns
  head_shankWidth   ; microns
Channel 5, e.g.:
  ch5_s   ; shank index
  ch5_x   ; microns from left edge of shank
  ch5_z   ; microns from center of tip-most electrode row
  ch5_u   ; used-flag (in CAR operations)
Note: Fields are sorted into ascending alphanumeric order.

-cmd=getImecChanGains -args=ip\nchan
Returns lines: AP and LF gain for given probe and channel.

-cmd=getParams
Get the most recently used run parameters.
Returns lines: key=value.

-cmd=getParamsImecCommon
Get imec parameters common to all enabled probes.
Returns lines: key=value.

-cmd=getParamsImecProbe [-args=ip] (default 0)
Get imec parameters for given logical probe.
Returns lines: key=value.

-cmd=getParamsOneBox [-args=ip] (default 0)
Get parameters for given logical OneBox.
Returns lines: key=value.

-cmd=getProbeList
Returns string: (probeID,nShanks,partNumber)()...
- A parenthesized entry for each selected probe.
- probeID: zero-based integer.
- nShanks: integer {1,4}.
- partNumber: string, e.g., NP1000.
- If no probes, return '()'.

-cmd=getRunName
Returns string: base run-name.

-cmd=getStreamAcqChans -args=js\nip
Returns lines: list of stream channel counts, as follows:
js = 0: NI channels: {MN,MA,XA,DW}.
js = 1: OB channels: {XA,DW,SY}.
js = 2: IM channels: {AP,LF,SY}.

-cmd=getStreamI16ToVolts -args=js\nip\nchan
Return multiplier converting 16-bit binary channel to volts.

-cmd=getStreamMaxInt -args=js\nip
Return largest positive integer value for selected stream.

-cmd=getStreamNP -args=js
Return number (np) of js-type substreams.
For the given js, ip has range [0..np-1].

-cmd=getStreamSampleRate -args=js\nip
Returns sample rate for given stream or zero if error.

-cmd=getStreamSaveChans -args=js\nip
Returns lines: list of saved channels.

-cmd=getStreamSN -args=js\nip
Return type and serial number for stream.
js = 1: Get OneBox slot and SN.
js = 2: Get probe  type and SN.
SN = serial number string.

-cmd=getStreamVoltageRange -args=js\nip
Returns lines: voltage range for given stream.

-cmd=getTime
Returns number of seconds since SpikeGLX launched.

-cmd=getVersion
Returns SpikeGLX version string.

-cmd=isInitialized
Returns 1 if SpikeGLX is launched and ready to run.

-cmd=isRunning
Returns 1 if SpikeGLX is acquiring data.

-cmd=isSaving
Returns 1 if SpikeGLX is writing file data.

-cmd=isUserOrder -args=js\nip
Returns 1 if main Graphs window set to User order for stream.

-cmd=opto_emit -args=ip\ncolor\nsite
Direct emission to specified site (-1=dark).
ip:    imec probe index.
color: {0=blue, 1=red}.
site:  [0..13], or, -1=dark.

-cmd=opto_getAttenuations -args=ip\ncolor
Return list of 14 (double) site power attenuation factors.
ip:    imec probe index.
color: {0=blue, 1=red}.

-cmd=setAnatomy_Pinpoint -args=string
Set anatomy data string with Pinpoint format:
[probe-id,shank-id](startpos,endpos,R,G,B,rgnname)(startpos,endpos,R,G,B,rgnname)…()
- probe-id: SpikeGLX logical probe id.
- shank-id: [0..n-shanks].
- startpos: region start in microns from tip.
- endpos:   region end in microns from tip.
- R,G,B:    region color as RGB, each [0..255].
- rgnname:  region name text.

-cmd=setAudioEnable -args=1
Set audio output on/off. Note that this command has
no effect if not currently running.

-cmd=setAudioParams -args=group\nkey=value\nkey=value...
Set subgroup of parameters for audio-out operation. Parameters
are a map of key=value pairs. This call stops current output.
Call setAudioEnable() to restart it.

-cmd=setDataDir -args=idir\npath
Set ith global data directory.
Set required parameter idir to zero for main data directory.

-cmd=setDigitalOut -args=1\nchanString
Set digital output high/low. Channel strings have form:
"Dev6/port0/line2,Dev6/port0/line5".

-cmd=setMetadata -args=key=value\nkey=value...
If a run is in progress, set metadata to be added to the
next output file-set. Metadata must be in the form of a
map of key=value pairs.

-cmd=setMultiDriveEnable -args=1
Set multi-drive run-splitting on/off.

-cmd=setNextFileName -args=name
For only the next trigger (file writing event) this overrides
all auto-naming, giving you complete control of where to save
the files, the file name, and what g- and t-indices you want
(if any). For example, regardless of the run's current data dir,
run name and indices, if you set: 'otherdir/yyy_g5/yyy_g5_t7',
SpikeGLX will save the next files in flat directory yyy_g5/:
   - otherdir/yyy_g5/yyy.g5_t7.nidq.bin,meta
   - otherdir/yyy_g5/yyy.g5_t7.imec0.ap.bin,meta
   - otherdir/yyy_g5/yyy.g5_t7.imec0.lf.bin,meta
   - otherdir/yyy_g5/yyy.g5_t7.imec1.ap.bin,meta
   - otherdir/yyy_g5/yyy.g5_t7.imec1.lf.bin,meta
   - etc.
- The destination directory must already exist...No parent directories
or probe subfolders are created in this naming mode.
- The run must already be in progress.
- Neither the custom name nor its indices are displayed in the Graphs
window toolbars. Rather, the toolbars reflect standard auto-names.
- After writing this file set, the override is cleared and auto-naming
will resume as if you never called setNextFileName. You have to call
setNextFileName before each trigger event to create custom trial series.
For example, you can build a software-triggered t-series using sequence:
   + setNextFileName( 'otherdir/yyy_g0/yyy_g0_t0' )
   + setRecordingEnable( 1 )
   + setRecordingEnable( 0 )
   + setNextFileName( 'otherdir/yyy_g0/yyy_g0_t1' )
   + setRecordingEnable( 1 )
   + setRecordingEnable( 0 )
   + etc.

-cmd=setParams -args=key=value\nkey=value...
The inverse of getParams, this sets run parameters.
Alternatively, you can pass the parameters to startRun,
which calls this in turn. Run parameters are a map of
key=value pairs. The call will error if a run is currently
in progress.
Note: You can set any subset of [DAQSettings].

-cmd=setParamsImecCommon -args=key=value\nkey=value...
The inverse of getParamsImecCommon, this sets parameters
common to all enabled probes. Parameters are a map of
key=value pairs. The call will error if a run is currently
in progress.
Note: You can set any subset of [DAQ_Imec_All].

-cmd=setParamsImecProbe -args=ip\nkey=value\nkey=value...
The inverse of getParamsImecProbe, this sets parameters
for a given logical probe. Parameters are a map of
key=value pairs. The call will error if file writing
is currently in progress.
Note: You can set any subset of fields under [SerialNumberToProbe]/SNjjj.

-cmd=setParamsOneBox -args=ip\nkey=value\nkey=value...
The inverse of getParamsOneBox, this sets parameters
for a given logical OneBox. Parameters are a map of
key=value pairs. The call will error if a run is currently
in progress.
Note: You can set any subset of fields under [SerialNumberToOneBox]/SNjjj.

-cmd=setRecordingEnable -args=1
Set gate (file writing) on/off during run.
When auto-naming is in effect, opening the gate advances
the g-index and resets the t-index to zero. Auto-naming is
on unless setNextFileName has been used to override it.

-cmd=setRunName -args=name
Set the run name for the next time files are created
(either by trigger, setRecordingEnable() or by startRun()).

-cmd=setTriggerOffBeep -args=hertz\nmillisec
During a run, set frequency and duration of Windows
beep signaling file closure. hertz=0 disables the beep.
Parameters hertz, millisec are integers.

-cmd=setTriggerOnBeep -args=hertz\nmillisec
During a run set frequency and duration of Windows
beep signaling file creation. hertz=0 disables the beep.
Parameters hertz, millisec are integers.

-cmd=startRun [-args=run-name]
Start data acquisition run. Last-used parameters remain
in effect. An error is flagged if already running.

-cmd=stopRun
Unconditionally stop current run, close data files
and return to idle state.

-cmd=triggerGT -args=g\nt
Using standard auto-naming, set both the gate (g) and
trigger (t) levels that control file writing.
  -1 = no change.
   0 = set low.
   1 = increment and set high.
E.g., triggerGT( -1, 1 ) = same g, increment t, start writing.
- triggerGT only affects the 'Remote controlled' gate type
and/or the 'Remote controlled' trigger type.
- The 'Enable Recording' button, when shown, is a master override
switch. triggerGT is blocked until you click the button or call
setRecordingEnable.


Change Log
----------
Version 1.3
- Use CPP-SDK v1.2.

Version 1.2
- Add getGeomMap.

Version 1.1
- Better demos.

Version 1.0
- Initial release.


