Any time you make a modification to the database you need to include a migration script which applies those changes.

On boot-up, the server will look for which version your database is and then apply all migration scripts it's missing. This includes creating the database if it doesn't exist yet.

When naming your migration scripts, the established format is:

<UTC Date>.<Version number of the day>.sql

Example:

2018-10-22.1 for the first migration made on 2018-10-22.

Migration scripts are applied IN ASCENDING ORDER of the date and version number (oldest to newest, lowest version to highest)

You should *NEVER* touch the Initialization.sql script. If you need to remove a database object or make some other change you must apply it in a migration script.

Final note: The files must be EMBEDDED RESOURCES in order for the app to pick them up. The folder in Visual Studio should do this automatically but if not, be sure to embed it manually.