"""Material budgets reuse the existing linear/color/normal-filtered mip bytes."""
from pathlib import Path
import struct
import sys
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from TrellisNwnMaterial import trim_dds_mips


def fixture(fourcc=b'DXT1',width=16,height=8):
    header=bytearray(128);header[:4]=b'DDS ';struct.pack_into('<I',header,4,124)
    struct.pack_into('<I',header,8,0xa1007);struct.pack_into('<II',header,76,32,4)
    struct.pack_into('<II',header,12,height,width);header[84:88]=fourcc
    block=8 if fourcc in [b'DXT1',b'ATI1'] else 16;levels=[];w,h=width,height
    while True:
        levels.append(bytes([len(levels)+1])*(max(1,(w+3)//4)*max(1,(h+3)//4)*block))
        if max(w,h)==1:break
        w,h=max(1,w//2),max(1,h//2)
    struct.pack_into('<I',header,20,len(levels[0]));struct.pack_into('<I',header,28,len(levels))
    struct.pack_into('<I',header,108,0x401008 if len(levels)>1 else 0x1000)
    return bytes(header)+b''.join(levels),levels


class MaterialMipBudgetTests(unittest.TestCase):
    def test_each_bc_format_retains_every_remaining_mip_byte(self):
        for fourcc in [b'DXT1',b'DXT5',b'ATI1',b'ATI2']:
            with self.subTest(fourcc=fourcc):
                source,levels=fixture(fourcc);after,report=trim_dds_mips(source,4)
                self.assertEqual(after[128:],b''.join(levels[2:]))
                self.assertEqual(struct.unpack_from('<II',after,12),(2,4))
                self.assertEqual(struct.unpack_from('<I',after,28)[0],len(levels)-2)
                self.assertEqual(report['bytes_saved'],sum(map(len,levels[:2])))
                self.assertEqual(after[84:88],fourcc)
                self.assertEqual(trim_dds_mips(after,4)[0],after)

    def test_small_constant_map_is_not_upscaled(self):
        source,_=fixture(width=4,height=4);after,report=trim_dds_mips(source,256)
        self.assertEqual(source,after);self.assertEqual(0,report['bytes_saved'])

    def test_single_pixel_zero_mip_count_is_a_complete_unchanged_chain(self):
        for fourcc in [b'DXT1',b'DXT5',b'ATI1',b'ATI2']:
            with self.subTest(fourcc=fourcc):
                source,_=fixture(fourcc,width=1,height=1);source=bytearray(source)
                struct.pack_into('<I',source,28,0);struct.pack_into('<I',source,8,0x81007)
                after,report=trim_dds_mips(bytes(source),256)
                self.assertEqual(bytes(source),after);self.assertEqual(0,report['bytes_saved'])

    def test_rejects_incomplete_non_bc_and_non_2d_data(self):
        source,_=fixture()
        broken=[]
        for offset,value in [(4,0),(28,1),(12,7),(112,512),(24,2),(76,0),(76,31),(80,0),(80,64)]:
            data=bytearray(source);struct.pack_into('<I',data,offset,value);broken.append(bytes(data))
        data=bytearray(source);data[84:88]=b'DX10';broken.append(bytes(data));broken.append(source[:-1])
        for data in broken:
            with self.subTest(header=data[:32]),self.assertRaises(ValueError):trim_dds_mips(data,4)

    def test_rejects_invalid_budget_values(self):
        source,_=fixture()
        for budget in [True,0,2,3,12,256.0]:
            with self.subTest(budget=budget),self.assertRaises(ValueError):trim_dds_mips(source,budget)


if __name__=='__main__':unittest.main()
