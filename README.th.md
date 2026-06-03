# <img src="PriconneReALLTLInstaller/Resources/jewel.ico" width="28"> PriconneReALLTL Installer

[![Latest release](https://img.shields.io/github/v/release/HetCreep/PriconneReALLTL-Installer?sort=semver&display_name=tag)](https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest)
[![Build](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/release.yml/badge.svg)](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/release.yml)
[![CodeQL](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/codeql.yml/badge.svg)](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/codeql.yml)
![License: MIT](https://img.shields.io/badge/license-MIT-green)
![.NET Framework 4.8](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)

**📖 อ่านเป็นภาษา:** [English](README.md) · ไทย

โปรแกรม GUI (WinForms) สำหรับติดตั้ง/อัปเดตแพตช์แปลภาษา BepInEx ของเกม **Princess Connect! Re:Dive** — รองรับ **แหล่งแปลหลายภาษาที่ผู้ใช้เลือกเองได้ (English / ไทย)** พร้อมช็อตคัต *อัปเดต + เล่น* ในคลิกเดียว

**"ReALLTL"** = รองรับแพตช์แปล **ทุกภาษา** เป็น fork ที่แยกขาดสมบูรณ์และรีแบรนด์มาจาก [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) โดยมีอัตลักษณ์ของตัวเอง (ชื่อ, GUID, strong-name key, URL self-update) เพื่อไม่ให้ตัวติดตั้งทั้งสองชนกัน

---

## 🌟 ฟีเจอร์

* **เลือกแหล่งแปลได้ (English / ไทย)** — สลับระหว่าง [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) กับ [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) ได้จากหน้าหลักเลย ทุก URL ของแพตช์อิงตามแหล่งที่เลือก — เพิ่มภาษาใหม่ = เพิ่มรายการเดียวในลิสต์
* **เปิดเกมผ่านช็อตคัตแบบ wrap** — ตัว GUI เน้นที่การแพตช์; ปุ่ม **Launch Game** ในแอปเปิดเกมผ่าน DMM ถ้าต้องการเล่น *แยกตามบัญชี* หรือ *อัปเดต + เล่น* คลิกเดียว ให้ **wrap** ช็อตคัต launcher ที่**ชี้ไปที่ตัวเกม** ([DMMGamePlayerFastLauncher](https://github.com/fa0311/DMMGamePlayerFastLauncher), หรือช็อตคัตบัญชีของ [PriconneMultiAccountLauncher](https://github.com/HetCreep/PriconneMultiAccountLauncher)) กดช็อตคัตที่ wrap แล้ว → อัปเดตแพตช์ก่อน แล้วเปิดเป้าหมายนั้นทันที — แยกตามบัญชี ย้อนกลับได้ (un-wrap คืนค่าเดิม) *ถ้าเปิดผ่าน DMM ธรรมดา ให้ใช้ **AutoUpdater shortcut** แทน (ดูด้านล่าง) — การ wrap ช็อตคัต DMM เปล่า ๆ จะแค่เปิดตัว DMM launcher ซ้ำ ไม่ได้เข้าเกมตรง*
* **รองรับ PriconneMultiAccountLauncher** — ตรวจจับอัตโนมัติผ่าน Inno Setup uninstall key (HKCU/HKLM) พร้อม fallback ที่ `%APPDATA%`
* **Modloader ปักหมุด source ที่ ImaterialC** — BepInEx IL2CPP interop มาจาก repo canonical ของ ImaterialC (release ล่าสุด) เสมอ ไม่ขึ้นกับแหล่งแปลที่เลือก
* **ติดตั้งปลอดภัย + ถอนสะอาด** — ไฟล์แพตช์ `.zip` ถูก **ตรวจ SHA-256** เทียบกับ digest ที่ GitHub เผยแพร่ก่อนแตะไฟล์ใด ๆ; การแตกไฟล์มี **zip-slip guard**; การถอนเป็นแบบ **ref-counted** (ถ้าลงทั้ง English และ ไทย **ผ่านแอปนี้** การถอนภาษาหนึ่งจะคงอีกภาษากับ modloader ที่ใช้ร่วมกันไว้) และ **path-guard** อยู่ในโฟลเดอร์เกมเท่านั้น
* **เป็นมิตรกับ rate-limit** — การเช็คเวอร์ชั่น patch/modloader ถูก cache ~6 ชม. (การเช็ค self-update ของตัวติดตั้งเอง ~7 วัน) ดังนั้น GitHub API token จึง *ไม่บังคับ* หากตั้งค่าไว้ token จะถูกเก็บแบบเข้ารหัส (Windows DPAPI), ปิดบังบนหน้าจอ, และไม่ถูกบันทึกลง log
* **บิลด์บนคลาวด์ที่ตรวจสอบได้** — บิลด์โดย GitHub Actions บน tag `v*` **เท่านั้น**; ทุก release แนบ **`SHA256SUMS.txt`** เซ็นแบบ strong-name (ยังไม่ได้เซ็น Authenticode — ให้ตรวจด้วย SHA-256 ดูด้านล่าง)

---

## 💾 ดาวน์โหลด

**[Release ล่าสุด →](https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest)**

แต่ละ release มีให้เลือก 2 แบบ:

| ไฟล์ | คืออะไร |
|---|---|
| **`PriconneReALLTLInstaller-<version>.exe`** | แบบพกพา ไฟล์เดียว — รันที่ไหนก็ได้ ไม่ต้องติดตั้ง และเป็นไฟล์เดียวกับที่ self-update ดาวน์โหลดมา |
| **`PriconneReALLTLInstaller-<version>-Setup.exe`** | ตัวติดตั้งแบบ per-user (ไม่ต้อง admin) เพิ่มช็อตคัต Start Menu และรายการถอนการติดตั้งที่ล้าง cache/settings ของแอปด้วย; self-update ยังทำงาน (ติดตั้งใต้โปรไฟล์ผู้ใช้) |

เลือกแบบไหนก็ได้ — exe พกพาถ้าแค่อยากรัน, Setup ถ้าชอบมี Start Menu และถอนสะอาด

---

## ✅ ตรวจสอบไฟล์ที่ดาวน์โหลด

ทุก release แนบ **`SHA256SUMS.txt`** และระบุค่า SHA-256 ไว้ใน release notes ด้วย ตรวจไฟล์ที่โหลดมา:

```powershell
Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.4.exe
```

เทียบผลกับค่าใน `SHA256SUMS.txt` / release notes — ต้องตรงกันเป๊ะ

> ไฟล์ถูก **เซ็น strong-name แต่ยังไม่ได้เซ็น Authenticode** ดังนั้น Windows **SmartScreen** อาจขึ้นเตือน *"Windows protected your PC"* ตอนเปิดครั้งแรก กด **More info → Run anyway** เฉพาะเมื่อค่า SHA-256 ตรงกันเท่านั้น (การเซ็น Authenticode เป็นแผนปรับปรุงในอนาคต)

ดาวน์โหลดจาก **[หน้า Releases ทางการ](https://github.com/HetCreep/PriconneReALLTL-Installer/releases)** เท่านั้น — อย่าโหลดจาก mirror, Discord, หรือข้อความส่วนตัว

---

## ⚙️ ควรรู้

- **การเปิดเกม** ปุ่ม **Launch Game** หลักเปิดเกมผ่าน **DMM** — **ไม่ได้** อัปเดตแพตช์ก่อน ถ้าต้องการ *อัปเดต + เล่น* คลิกเดียว ให้ใช้ช็อตคัตที่อัปเดตให้: **wrap** ช็อตคัต launcher ที่ชี้ไปที่ตัวเกม (DMMGamePlayerFastLauncher หรือบัญชีของ PriconneMultiAccountLauncher) — หรือถ้าเปิดผ่าน **DMM ธรรมดา** ใช้ **Create AutoUpdater Shortcut** สองแบบนี้จะอัปเดตแพตช์ก่อน แล้วค่อยเปิดเกม (wrap ย้อนกลับได้)
- **เมนู Settings** นอกจาก toggle เช็คอัปเดตตอนเปิดโปรแกรมแล้ว ยังมี: **Clear Download Cache**, **Edit Ignored Files** (กันไฟล์ถูกเขียนทับ/ลบตอนอัปเดต), **Import / Export Settings**, **GitHub API Settings** (token ทางเลือก), และ **Rate-Limit Info**
- **การถอนการติดตั้ง** การถอนแอป (Windows / Setup uninstall) จะล้างข้อมูล *ของแอปเอง* — settings, token, cache, log — แต่ **ไม่ลบแพตช์ออกจากเกม** ให้รันคำสั่ง **Uninstall** ในแอปก่อนเพื่อลบตัวแปลออก *แล้วค่อย* ถอนแอป

## 🔐 ความเป็นส่วนตัว & ความปลอดภัย

- **[PRIVACY.md](PRIVACY.md)** — ไม่มี telemetry; รายการ host ขาออก (GitHub เท่านั้น) แบบครบถ้วน
- **[SECURITY.md](SECURITY.md)** — แนวทางความปลอดภัย, สิ่งที่ "ไม่ทำเด็ดขาด" (Hard No's), และวิธีรายงานช่องโหว่แบบ private

---

## ⚠️ ข้อจำกัดความรับผิด (Disclaimer)

นี่เป็นเครื่องมือ **ไม่เป็นทางการ ทำขึ้นโดยแฟน ๆ** **ไม่ได้สังกัด ไม่ได้รับการรับรอง และไม่เกี่ยวข้องกับ** Cygames, Inc., DMM, หรือ tynave *Princess Connect! Re:Dive* รวมถึงชื่อและทรัพย์สินที่เกี่ยวข้องทั้งหมดเป็นเครื่องหมายการค้า/ทรัพย์สินของเจ้าของนั้น ๆ ใช้ในที่นี้เพื่อ **อ้างถึงเชิงพรรณนา (nominative)** เพื่อระบุชื่อเท่านั้น

ตัวติดตั้งนี้นำ **ม็อดแปลภาษาของบุคคลที่สาม** (ผ่าน BepInEx mod loader) ไปติดตั้งลงในเกมของคุณ **การดัดแปลงเกมอาจละเมิดเงื่อนไขการให้บริการ (ToS) และอาจทำให้บัญชีถูกระงับหรือแบนได้** คุณติดตั้งและใช้การดัดแปลงเหล่านี้ **ด้วยความเสี่ยงของคุณเอง**

ซอฟต์แวร์นี้ให้บริการ **"ตามสภาพ (AS IS)" โดยไม่มีการรับประกันใด ๆ ทั้งสิ้น** ผู้พัฒนา **ไม่รับผิดชอบใด ๆ** ต่อการดำเนินการต่อบัญชี การสูญหายของข้อมูล หรือความเสียหายที่เกิดจากการใช้งาน ดู [LICENSE](LICENSE.txt)

---

## 🛠️ เครดิต & การให้เครดิต

เป็น fork ภายใต้สัญญาอนุญาต MIT ขอบคุณอย่างยิ่งต่อ:

* [PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) โดย [tynave](https://github.com/tynave) — ตัวติดตั้งต้นทางที่ fork มา
* [PriconeTL_Updater](https://github.com/touanu/PriconeTL_Updater) โดย [touanu](https://github.com/touanu) — แรงบันดาลใจดั้งเดิม
* แพตช์แปล: [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) · [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย)
* ทรัพย์สินภายในเกมทั้งหมดเป็นของ CyberAgent, Inc. / Cygames, Inc. และผู้สร้างนั้น ๆ

### 🤖 สร้างด้วย AI Co-Engineers

- **Claude Code** (Anthropic) — รีแบรนด์ + แยกขาดโค้ด C#, ระบบ multi-source English/ไทย, สถาปัตยกรรม shortcut-wrap launch, การตรวจเวอร์ชั่นต่อแหล่ง, rate-limit caching, build/CI
- **Antigravity** (Google DeepMind) — UI multi-launcher แบบ native ในยุคแรก, การปิด ClickOnce, และการคอมไพล์บนคลาวด์ด้วย GitHub Actions
