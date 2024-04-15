%% MATLAB PR701 Remote Control
port = instrfindall;
if ~isempty(port)
    fclose(port);
    delete(port);
    clear port;
end

port = serial('COM4','BaudRate',9600,'DataBits',8,'Parity','none','StopBits',1,'FlowControl','hardware','InputBufferSize',4096);
fopen(port);
fprintf(port,['PR701']);   % Initiate Remote Control
pause(3)
fprintf(port,'S,,,,,1000,0,1,0,0,0');  % Set Integration Time (1000ms).
pause(2)

n = get(port,'BytesAvailable');
if n > 0
    bout = fread(port,n);
end
sprintf('%c',bout)
