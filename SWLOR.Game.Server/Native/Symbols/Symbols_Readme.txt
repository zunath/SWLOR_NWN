Pull symbols using the below Linux command on the Linux binary of nwserver. This needs to be done after every update to the base NWN game.
After doing this, check our Native overrides to ensure the function names remain the same. Update them if not.

nm -gD nwserver-linux > symbols.txt