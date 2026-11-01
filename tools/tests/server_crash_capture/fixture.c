#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

static volatile sig_atomic_t shutdown_requested;

static void shutdown_server(int signal_number)
{
    shutdown_requested = signal_number;
}

void nwnx_signal_handler(int signal_number)
{
    (void)signal_number;
    for (;;) pause();
}

__attribute__((noinline)) static void trigger_crash(void)
{
    *(volatile int *)0 = 1;
}

int main(int argc, char **argv)
{
    const char *mode = argc > 1 ? argv[1] : "normal";
    signal(SIGSEGV, nwnx_signal_handler);
    signal(SIGINT, shutdown_server);
    signal(SIGTERM, shutdown_server);
    puts("fixture ready");
    fflush(stdout);
    if (!strcmp(mode, "crash")) {
        trigger_crash();
    } else if (!strcmp(mode, "wait")) {
        while (!shutdown_requested) pause();
    } else if (!strcmp(mode, "restart")) {
        sleep(11);
    } else if (!strcmp(mode, "arguments")) {
        return argc == 3 && !strcmp(argv[2], "a space 'quote' $literal ; value") ? 42 : 43;
    }
    return 0;
}
