# <img src="PriconneReALLTLInstaller/Resources/jewel.ico" width="28"> PriconneReALLTL Installer

[![Latest release](https://img.shields.io/github/v/release/HetCreep/PriconneReALLTL-Installer?sort=semver&display_name=tag)](https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest)
[![Build](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/release.yml/badge.svg)](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/release.yml)
[![CodeQL](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/codeql.yml/badge.svg)](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/codeql.yml)
![License: MIT](https://img.shields.io/badge/license-MIT-green)
![.NET Framework 4.8](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)

**📖 Đọc bằng ngôn ngữ:** [English](README.md) · [ไทย](README.th.md) · Tiếng Việt

Công cụ cài đặt/cập nhật (WinForms GUI) cho các bản dịch BepInEx của **Princess Connect! Re:Dive** — hỗ trợ **nhiều nguồn dịch do người dùng tự chọn (English / ไทย / Tiếng Việt)** và shortcut *cập nhật + chơi* chỉ một cú nhấp.

**"ReALLTL"** = hỗ trợ **tất cả** các bản dịch. Đây là fork đã tách hoàn toàn và đổi thương hiệu từ [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer), có danh tính riêng (tên, GUID, strong-name key, URL self-update) để hai trình cài đặt không bao giờ xung đột.

---

## 🌟 Tính năng

* **Chọn nguồn dịch (English / ไทย / Tiếng Việt)** — chuyển đổi giữa [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English), [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) và [NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN) (Tiếng Việt) ngay trên màn hình chính. Mọi URL của patch đều dựa theo nguồn đã chọn — thêm một ngôn ngữ mới chỉ là thêm một dòng trong danh sách. *Lưu ý: bản dịch tiếng Việt được tạo bằng Gemini AI (theo chia sẻ của tác giả) — một thông báo về chất lượng sẽ hiện ra trước khi bạn chọn nguồn này. Patch VN chỉ chứa file text, nên khi cài, installer sẽ tự tải thêm modloader engine từ ImaterialC (~330 MB) làm lớp nền.*
* **Mở game qua shortcut đã wrap** — GUI tập trung vào việc vá; nút **Launch Game** trong app mở game qua DMM. Nếu muốn chơi *theo từng tài khoản* hoặc *cập nhật + chơi* một cú nhấp, hãy **wrap** một shortcut launcher trỏ thẳng vào game ([DMMGamePlayerFastLauncher](https://github.com/fa0311/DMMGamePlayerFastLauncher), hoặc shortcut tài khoản của [PriconneMultiAccountLauncher](https://github.com/HetCreep/PriconneMultiAccountLauncher)): nhấn shortcut đó → patch được cập nhật trước, rồi game mở đúng mục tiêu ấy. Có thể đảo ngược (un-wrap trả lại như cũ). *Mở qua DMM thường? Dùng **AutoUpdater shortcut** (xem bên dưới) — wrap một shortcut DMM trần chỉ mở lại DMM launcher chứ không vào game.*
* **Tích hợp PriconneMultiAccountLauncher** — tự phát hiện qua Inno Setup uninstall key (HKCU/HKLM), kèm fallback tại `%APPDATA%`.
* **Modloader cố định nguồn ImaterialC** — BepInEx IL2CPP interop luôn lấy từ repo chính thức của ImaterialC (release mới nhất), bất kể bạn chọn nguồn dịch nào.
* **Cài an toàn & gỡ sạch** — file patch `.zip` được **kiểm tra SHA-256** so với digest GitHub công bố trước khi chạm vào bất kỳ file nào; giải nén có **zip-slip guard**; gỡ cài đặt theo cơ chế **ref-counted** (cài hai nguồn **qua app này**, gỡ một nguồn vẫn giữ nguyên nguồn kia và modloader dùng chung) và **path-guard** chỉ trong thư mục game.
* **Thân thiện với rate-limit** — kết quả kiểm tra phiên bản patch/modloader được cache ~6 giờ (self-update của chính installer ~7 ngày), nên GitHub API token là *tuỳ chọn*. Nếu đặt, token được lưu mã hoá (Windows DPAPI), che trên màn hình và không bao giờ ghi vào log.
* **Build trên cloud, kiểm chứng được** — chỉ build bởi GitHub Actions trên tag `v*`; mỗi release đính kèm **`SHA256SUMS.txt`**. Đã ký strong-name (chưa ký Authenticode — hãy kiểm tra bằng SHA-256, xem bên dưới).

---

## 💾 Tải về

**[Release mới nhất →](https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest)**

Mỗi release có hai lựa chọn:

| File | Là gì |
|---|---|
| **`PriconneReALLTLInstaller-<version>.exe`** | Bản portable, một file duy nhất — chạy ở đâu cũng được, không cần cài. Đây cũng là file mà self-update tích hợp sẽ tải về. |
| **`PriconneReALLTLInstaller-<version>-Setup.exe`** | Trình cài đặt per-user (không cần admin). Thêm shortcut Start Menu và mục gỡ cài đặt (gỡ kèm cache/settings của app). Self-update vẫn hoạt động (cài dưới hồ sơ người dùng). |

Chọn bản nào cũng được — exe portable nếu chỉ muốn chạy ngay, Setup nếu thích có Start Menu và gỡ sạch sẽ.

---

## ✅ Kiểm tra file đã tải

Mỗi release đính kèm **`SHA256SUMS.txt`** và ghi SHA-256 ngay trong release notes. Kiểm tra file bạn tải về:

```powershell
Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-*.exe
```

So sánh kết quả với giá trị trong `SHA256SUMS.txt` / release notes — phải khớp tuyệt đối.

> File đã **ký strong-name nhưng chưa ký Authenticode**, nên Windows **SmartScreen** có thể hiện *"Windows protected your PC"* lần chạy đầu. Chỉ chọn **More info → Run anyway** khi SHA-256 khớp. Ký Authenticode là cải tiến đã nằm trong kế hoạch.

Chỉ tải từ **[trang Releases chính thức](https://github.com/HetCreep/PriconneReALLTL-Installer/releases)** — không bao giờ tải từ mirror bên thứ ba, Discord hay tin nhắn riêng.

---

## ⚙️ Nên biết

- **Mở game.** Nút **Launch Game** chính mở game qua **DMM** — nó **không** cập nhật patch trước. Muốn **cập nhật + chơi** một cú nhấp, dùng shortcut có làm việc đó: **wrap** một shortcut launcher trỏ vào game (DMMGamePlayerFastLauncher hoặc tài khoản PriconneMultiAccountLauncher), hoặc — nếu mở qua **DMM thường** — dùng **Create AutoUpdater Shortcut**. Cả hai đều cập nhật patch trước rồi mới mở game; wrap có thể đảo ngược. (Wrap một shortcut DMM trần chỉ mở lại DMM launcher, nên người dùng DMM hãy chọn AutoUpdater shortcut.)
- **Menu Settings.** Ngoài nút bật/tắt kiểm tra cập nhật khi khởi động, còn có: **Clear Download Cache**, **Edit Ignored Files** (bảo vệ file khỏi bị ghi đè/xoá khi cập nhật), **Import / Export Settings**, **GitHub API Settings** (token tuỳ chọn), và **Rate-Limit Info**.
- **Gỡ cài đặt.** Gỡ app (Windows / Setup uninstall) chỉ xoá dữ liệu *của chính app* — settings, token, cache, log — nhưng **không gỡ patch khỏi game**. Hãy chạy thao tác **Uninstall** trong app trước để gỡ bản dịch, *rồi mới* gỡ app.

## 🔐 Quyền riêng tư & Bảo mật

- **[PRIVACY.md](PRIVACY.md)** — không telemetry; danh sách host kết nối ra ngoài (chỉ GitHub) đầy đủ.
- **[SECURITY.md](SECURITY.md)** — lập trường bảo mật, những điều "tuyệt đối không làm" (Hard No's), và cách báo cáo lỗ hổng một cách riêng tư.

---

## ⚠️ Miễn trừ trách nhiệm (Disclaimer)

Đây là công cụ **không chính thức, do fan làm**. Nó **không liên kết, không được chứng thực, và không liên quan đến** Cygames, Inc., DMM, hay tynave. *Princess Connect! Re:Dive* cùng mọi tên gọi và tài sản liên quan là thương hiệu / tài sản của chủ sở hữu tương ứng, được dùng ở đây **chỉ để định danh (nominative)**.

Trình cài đặt này đưa **mod dịch của bên thứ ba** (qua BepInEx mod loader) vào game của bạn. **Việc chỉnh sửa game có thể vi phạm Điều khoản Dịch vụ và có thể khiến tài khoản bị đình chỉ hoặc khoá.** Bạn cài và sử dụng các chỉnh sửa này **với rủi ro do chính bạn chịu**.

Phần mềm được cung cấp **"NGUYÊN TRẠNG (AS IS)", không kèm bất kỳ bảo đảm nào**. Các tác giả **không chịu trách nhiệm** cho bất kỳ hành động nào lên tài khoản, mất dữ liệu, hay thiệt hại phát sinh từ việc sử dụng. Xem [LICENSE](LICENSE.txt).

---

## 🛠️ Ghi công

Fork theo giấy phép MIT. Chân thành cảm ơn:

* [PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) của [tynave](https://github.com/tynave) — trình cài đặt gốc mà dự án này fork từ đó
* [PriconeTL_Updater](https://github.com/touanu/PriconeTL_Updater) của [touanu](https://github.com/touanu) — nguồn cảm hứng ban đầu
* Các bản dịch: [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) · [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) · [NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN) (Tiếng Việt)
* Mọi tài sản trong game thuộc về CyberAgent, Inc. / Cygames, Inc. và các tác giả tương ứng.

### 🤖 Xây dựng cùng AI Co-Engineers

- **Claude Code** (Anthropic) — đổi thương hiệu + tách mã C#, hệ multi-source English/ไทย/Tiếng Việt, kiến trúc shortcut-wrap launch, phát hiện phiên bản theo từng nguồn, cache rate-limit, build/CI.
- **Antigravity** (Google DeepMind) — UI multi-launcher native thời kỳ đầu, gỡ ClickOnce, và biên dịch cloud bằng GitHub Actions.
