"""Native instruction regression tests for DDS icon discovery.

Set NWN_EE_NWMAIN to the supported Windows nwmain.exe and install pefile/unicorn
to run the native checks. The original client is read only and never launched.
"""
import hashlib
import importlib.util
import os
from pathlib import Path
import struct
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("icon_patch", ROOT / "tools/PatchDdsInventoryIcons.py")
patch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(patch)
try:
    import pefile
    from unicorn import Uc, UC_ARCH_X86, UC_MODE_64, UC_HOOK_CODE
    from unicorn.x86_const import (
        UC_X86_REG_RIP, UC_X86_REG_RSP, UC_X86_REG_RAX, UC_X86_REG_RCX,
        UC_X86_REG_RDX, UC_X86_REG_R8, UC_X86_REG_R9, UC_X86_REG_R15,
    )
except ImportError:
    pefile = None


class PatchInputTests(unittest.TestCase):
    def test_unknown_executable_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "Unsupported or already modified"):
            patch.patch_image(b"MZ" + bytes(1024))


@unittest.skipUnless(pefile is not None and os.environ.get("NWN_EE_NWMAIN"),
                     "Requires pefile, unicorn and NWN_EE_NWMAIN")
class NativeIconLookupTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source_path = Path(os.environ["NWN_EE_NWMAIN"])
        cls.source = cls.source_path.read_bytes()
        cls.patched = patch.patch_image(cls.source)
        cls.original_pe = pefile.PE(data=cls.source)
        cls.patched_pe = pefile.PE(data=cls.patched)

    def select_requested_icon(self, executable, resref, resources):
        """Execute the real Windows client's composite lookup instructions.

        Only CExoResMan::Exists is stubbed with a resource index. Its volatile
        registers are deliberately clobbered to check the thunk's calling ABI.
        Stop at the existing requested-icon or default-icon branch, before GUI
        allocation/rendering. This proves discovery, not a GPU rendering result.
        """
        pe = executable
        base = pe.OPTIONAL_HEADER.ImageBase
        emulator = Uc(UC_ARCH_X86, UC_MODE_64)
        emulator.mem_map(base, patch.align(pe.OPTIONAL_HEADER.SizeOfImage, 4096))
        emulator.mem_write(base, pe.get_memory_mapped_image())
        scratch = 0x70000000
        emulator.mem_map(scratch, 0x20000)
        emulator.mem_write(scratch, resref.encode("ascii") + b"\0")
        emulator.reg_write(UC_X86_REG_R15, scratch)
        emulator.reg_write(UC_X86_REG_RSP, scratch + 0x18000)
        queries = []
        result = []

        def on_instruction(cpu, address, size, user_data):
            if address == base + patch.RESOURCE_EXISTS_RVA:
                self.assertEqual(cpu.reg_read(UC_X86_REG_RSP) % 16, 8)
                ptr = cpu.reg_read(UC_X86_REG_RDX)
                name = bytes(cpu.mem_read(ptr, 17)).split(b"\0")[0].decode("ascii")
                resource_type = cpu.reg_read(UC_X86_REG_R8) & 0xFFFF
                self.assertEqual(cpu.reg_read(UC_X86_REG_R9), 0)
                queries.append((name, resource_type))
                found = int((name, resource_type) in resources)
                for register in (UC_X86_REG_RCX, UC_X86_REG_RDX, UC_X86_REG_R8, UC_X86_REG_R9):
                    cpu.reg_write(register, 0xBAD)
                cpu.reg_write(UC_X86_REG_RAX, found)
                stack = cpu.reg_read(UC_X86_REG_RSP)
                return_address = struct.unpack("<Q", cpu.mem_read(stack, 8))[0]
                cpu.reg_write(UC_X86_REG_RSP, stack + 8)
                cpu.reg_write(UC_X86_REG_RIP, return_address)
            elif address in (base + 0x8406A5, base + 0x8406DE):
                result.append(address == base + 0x8406DE)
                cpu.emu_stop()

        emulator.hook_add(UC_HOOK_CODE, on_instruction)
        emulator.emu_start(base + 0x840689, base + 0x8406F3, count=1000)
        self.assertEqual(len(result), 1, "Lookup did not reach an existing client branch")
        self.assertEqual(emulator.reg_read(UC_X86_REG_RSP), scratch + 0x18000)
        return result[0], queries

    def test_reported_slots_reproduce_fallback_then_select_dds(self):
        for name in ("iwbwsh_m_111", "iwbwsh_m_201"):
            with self.subTest(name=name):
                resources = {(name, 2033)}
                self.assertFalse(self.select_requested_icon(self.original_pe, name, resources)[0])
                selected, queries = self.select_requested_icon(self.patched_pe, name, resources)
                self.assertTrue(selected)
                self.assertEqual(queries, [(name, 3), (name, 2033)])

    def test_every_shipped_dds_pistol_is_discoverable_without_tga(self):
        icons = list((ROOT / "SWLOR_Haks/sw_weapon").glob("iwbwsh_m_*.dds"))
        self.assertTrue(icons)
        for icon in icons:
            with self.subTest(icon=icon.stem):
                selected, _ = self.select_requested_icon(self.patched_pe, icon.stem, {(icon.stem, 2033)})
                self.assertTrue(selected)

    def test_existing_tga_behavior_is_preserved(self):
        name = "iwbwsh_m_011"
        for resources in ({(name, 3)}, {(name, 3), (name, 2033)}):
            selected, queries = self.select_requested_icon(self.patched_pe, name, resources)
            self.assertTrue(selected)
            self.assertEqual(queries, [(name, 3)])

    def test_native_dds_loader_accepts_every_repaired_icon(self):
        pe = self.original_pe
        base = pe.OPTIONAL_HEADER.ImageBase
        symbol = next(symbol for symbol in pe.DIRECTORY_ENTRY_EXPORT.symbols
                      if symbol.name == b"?GetChannelCount@CResDDS@@AEAAHI@Z")
        emulator = Uc(UC_ARCH_X86, UC_MODE_64)
        emulator.mem_map(base, patch.align(pe.OPTIONAL_HEADER.SizeOfImage, 4096))
        emulator.mem_write(base, pe.get_memory_mapped_image())
        scratch = 0x70000000
        emulator.mem_map(scratch, 0x10000)

        def native_channel_count(fourcc):
            stack = scratch + 0x8008
            return_address = scratch + 0x9000
            emulator.reg_write(UC_X86_REG_RSP, stack)
            emulator.reg_write(UC_X86_REG_RCX, scratch)
            emulator.reg_write(UC_X86_REG_RDX, int.from_bytes(fourcc, "little"))
            emulator.mem_write(stack, struct.pack("<Q", return_address))
            emulator.emu_start(base + symbol.address, return_address, count=100)
            return emulator.reg_read(UC_X86_REG_RAX)

        self.assertEqual(native_channel_count(bytes(4)), 0, "Original RGBA export is unsupported")
        icons = list((ROOT / "SWLOR_Haks/sw_weapon").glob("iwbwsh_m_*.dds"))
        self.assertTrue(icons)
        for icon in icons:
            with self.subTest(icon=icon.name):
                self.assertEqual(native_channel_count(icon.read_bytes()[84:88]), 4,
                                 "Native DDS loader must recognize RGBA channels")

    def test_missing_both_formats_retains_generic_fallback(self):
        selected, queries = self.select_requested_icon(self.patched_pe, "iwbwsh_m_255", set())
        self.assertFalse(selected)
        self.assertEqual(queries, [("iwbwsh_m_255", 3), ("iwbwsh_m_255", 2033)])

    def test_pe_integrity_and_unwind_table(self):
        pe = self.patched_pe
        original = self.original_pe
        self.assertEqual(hashlib.sha256(self.source_path.read_bytes()).hexdigest(), patch.SOURCE_SHA256)
        self.assertEqual(pe.OPTIONAL_HEADER.CheckSum, pe.generate_checksum())
        self.assertEqual(pe.OPTIONAL_HEADER.AddressOfEntryPoint, original.OPTIONAL_HEADER.AddressOfEntryPoint)
        self.assertEqual(pe.FILE_HEADER.NumberOfSections, original.FILE_HEADER.NumberOfSections + 1)
        section = pe.sections[-1]
        self.assertEqual(section.Name.rstrip(b"\0"), patch.SECTION_NAME)
        self.assertEqual(section.Characteristics, 0x60000020)
        thunk = patch.make_lookup_thunk(section.VirtualAddress)
        self.assertEqual(pe.get_data(section.VirtualAddress, len(thunk)), thunk)
        old_functions = [(entry.struct.BeginAddress, entry.struct.EndAddress, entry.struct.UnwindData)
                         for entry in original.DIRECTORY_ENTRY_EXCEPTION]
        functions = [(entry.struct.BeginAddress, entry.struct.EndAddress, entry.struct.UnwindData)
                     for entry in pe.DIRECTORY_ENTRY_EXCEPTION]
        self.assertEqual(functions[:-1], old_functions)
        start, end, unwind = functions[-1]
        self.assertEqual((start, end), (section.VirtualAddress, section.VirtualAddress + len(thunk)))
        self.assertEqual(pe.get_data(unwind, 8), bytes.fromhex("01 04 01 00 04 82 00 00"))
        # Original sections retain their bytes except the one verified call operand.
        for old_section in original.sections:
            before = bytearray(old_section.get_data())
            after = bytearray(pe.get_data(old_section.VirtualAddress, old_section.SizeOfRawData))
            call = patch.ICON_EXISTS_CALL_RVA - old_section.VirtualAddress
            if 0 <= call < len(before):
                self.assertEqual(after[call], 0xE8)
                after[call:call + 5] = before[call:call + 5]
            self.assertEqual(after, before, old_section.Name)

    def test_patched_executable_cannot_be_patched_again(self):
        with self.assertRaisesRegex(ValueError, "Unsupported or already modified"):
            patch.patch_image(self.patched)


if __name__ == "__main__":
    unittest.main()
