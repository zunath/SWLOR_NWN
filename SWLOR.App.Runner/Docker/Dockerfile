FROM nwnxee/unified:build8193.37.15
RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v vs2022 -l /remote_debugger/vsdbg

RUN chmod 777 -R /remote_debugger/vsdbg
