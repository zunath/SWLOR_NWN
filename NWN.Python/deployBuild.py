import os
import glob
import re
import winreg
import argparse
import shutil
import stat
from distutils.dir_util import copy_tree
from distutils.dir_util import remove_tree

# Helper functions
def find_file(name, path):
    for root, dirs, files in os.walk(path):
        if name in files:
            return os.path.join(root, name)

def remove_regex(path, regex = "*"):
    files = glob.glob(path + "\\" + regex)
    for f in files:
        os.remove(f)

def copy_folder_content(src, dst):
    if os.path.exists(dst):
        shutil.rmtree(dst)
    shutil.copytree(src, dst)

def remove_readonly_recursive(path):
    os.system("attrib -r \"" + path +"\\*.*\" /s /d")

# Find registery entry that has current user Document folder path
registry = winreg.ConnectRegistry(None, winreg.HKEY_CURRENT_USER)
registry_key = winreg.OpenKey(registry, r"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", 0, winreg.KEY_READ)
document_folder, regtype = winreg.QueryValueEx(registry_key, "Personal")
winreg.CloseKey(registry_key)
winreg.CloseKey(registry)
# Convert potential %userprofile% to real path
document_folder = os.path.expandvars(document_folder)

# NWN destination paths
nwn_destination_hd0 = None
nwn_destination_hak = None
nwn_destination_mod = None
nwn_destination_tlk = None
nwn_destination_bin = None

# Find nwn.ini in Documents folder
for folder in glob.glob(document_folder + "\\Neverwinter Nights*"):
    ini_file_location = find_file("nwn.ini", folder)
    if ini_file_location is not None:
        with open(ini_file_location, "r") as ini_file:
            lines = ini_file.readlines()
            # Get paths from nwn.ini
            for line in lines:
                if re.search(r'HD0=', line):
                    nwn_destination_hd0 = line.replace('HD0=','').replace("\n", "")
                    nwn_destination_bin = os.path.join(nwn_destination_hd0 + "\\dotnet")
                if re.search(r'HAK=', line):
                    nwn_destination_hak = line.replace('HAK=','').replace("\n", "")
                if re.search(r'TLK=', line):
                    nwn_destination_tlk = line.replace('TLK=','').replace("\n", "")
                if re.search(r'MODULES=', line):
                    nwn_destination_mod = line.replace('MODULES=','').replace("\n", "")

        ini_file.close()

ap = argparse.ArgumentParser()
# Create command line options for specific content copy
ap.add_argument("-c", "--content", required=False,
   help="path to project content folder")
ap.add_argument("-m", "--mod", required=False,
   help="path to project module folder")
ap.add_argument("-b", "--bin", required=False,
   help="path to project .net binaries folder")
ap.add_argument("-d", "--docker", required=False,
   help="path to project docker files folder")
   
args = ap.parse_args()

# Binaries copy
if None not in (args.bin, nwn_destination_bin):
    try:
        remove_readonly_recursive(nwn_destination_bin)
        copy_folder_content(args.bin, nwn_destination_bin)
    except:
        print("Binaries were not copied due to an error")
        pass
    else:
        print("Binaries copied successfully \nFrom: " + args.bin + "\nTo:   " + nwn_destination_bin)

# TODO: Implement other command line options

# Content copy
if None not in (args.content, nwn_destination_hak, nwn_destination_tlk):
    print("Content copy is not implemented")

# Module copy
if None not in (args.mod, nwn_destination_mod):
    print("Module copy is not implemented")

# Docker copy
if None not in (args.docker, nwn_destination_hd0):
    print("Docker copy is not implemented")