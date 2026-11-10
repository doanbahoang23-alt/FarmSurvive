# ⚔️ FarmSurvive - 2D Action RPG & Survival Farming Defense

[![Unity](https://img.shields.io/badge/Unity-6000.x%20(Unity%206)-black?logo=unity)](https://unity.com/)
[![Language](https://img.shields.io/badge/C%23-12.0-blue?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Code%20%7C%20SRP%20%7C%20Orchestrator-brightgreen)]()
[![Platform](https://img.shields.io/badge/Platform-PC%20%7C%20Windows-lightgrey)]()

> Một dự án game 2D kết hợp giữa **Hành động Nhập vai (Action RPG)**, **Canh tác Nông nghiệp (Farming)** và **Thủ thành Sinh tồn Vô tận (Endless Wave Defense)** trên nền tảng **Unity 6**. Dự án được xây dựng với kiến trúc mã nguồn chuẩn mực, tuân thủ nghiêm ngặt nguyên tắc Đơn nhiệm (SRP), Orchestrator Pattern, Hybrid 2D Pathfinding và Zero GC Allocation.

---

## 📌 Mục Lục
- [1. Giới Thiệu Dự Án](#1-giới-thiệu-dự-án)
- [2. Kiến Trúc & Design Patterns](#2-kiến-trúc--design-patterns)
- [3. Hệ Thống Tính Năng Nổi Bật](#3-hệ-thống-tính-năng-nổi-bật)
  - [3.1. Cơ Chế Chiến Đấu & Hit & Run](#31-cơ-chế-chiến-đấu--hit--run)
  - [3.2. AI Kẻ Địch Thuần 2D (Hybrid A* & Context Steering)](#32-ai-kẻ-địch-thuần-2d-hybrid-a--context-steering)
  - [3.3. Canh Tác Nông Nghiệp & Cây Trồng](#33-canh-tác-nông-nghiệp--cây-trồng)
  - [3.4. Hệ Thống Bẫy Thủ Thành & Hiệu Ứng Trạng Thái](#34-hệ-thống-bẫy-thủ-thành--hiệu-ứng-trạng-thái)
  - [3.5. Kinh Tế, Cửa Hàng & Túi Đồ](#35-kinh-tế-cửa-hàng--túi-đồ)
  - [3.6. Hệ Thống Lưu / Tải Game (Save & Load Multi-Slot)](#36-hệ-thống-lưu--tải-game-save--load-multi-slot)
- [4. Phím Tắt Điều Khiển (Controls)](#4-phím-tắt-điều-khiển-controls)
- [5. Cấu Trúc Thư Mục Dự Án](#5-cấu-trúc-thư-mục-dự-án)
- [6. Yêu Cầu Cấu Hình & Cài Đặt](#6-yêu-cầu-cấu-hình--cài-đặt)
- [7. Tác Giả](#7-tác-giả)

---

## 1. Giới Thiệu Dự Án

* **Tên dự án:** FarmSurvive
* **Engine:** Unity 6 (6000.x)
* **Tác giả:** Đoàn Hoàng
* **Thể loại:** 2D Top-down Action RPG / Survival / Farming & Base Defense.
* **Mô tả lối chơi:** Người chơi thu thập tài nguyên (gỗ, đá), cuốc đất, gieo hạt, chăm sóc mùa màng và mua sắm trang bị, bẫy phòng thủ từ Cửa Hàng. Sau mỗi 1 khoảng thời gian các đợt quái vật vô tận kéo đến tấn công cây trồng và người chơi. Người chơi phải kết hợp vũ khí cận chiến/tầm xa, đặt bẫy chiến thuật và kỹ năng Hit & Run để bảo vệ nông trại và sinh tồn qua các đợt sóng quái ngày càng hung hãn.

---

## 2. Kiến Trúc & Design Patterns

Dự án được thiết kế theo các tiêu chuẩn kỹ thuật chuyên nghiệp của Senior Unity Developer:

* **Orchestrator Pattern:** Các hàm vòng đời Unity (`Update`, `FixedUpdate`, `Awake`, `Start`) chỉ đóng vai trò là "nhạc trưởng" điều phối, phân nhánh qua các Guard Clauses sạch sẽ, tuyệt đối không nhồi nhét logic tính toán nặng nề.
* **Single Responsibility Principle (SRP):** Tách nhỏ các God Class và God Method thành các hàm chuyên biệt đơn nhiệm (ví dụ: `PlayerController` chia thành 4 nhóm độc lập: `HandleInput`, `HandleMovement`, `HandleStamina`, `HandleAnimation`).
* **Static Registry Pattern ($0\text{ GC Alloc}$):** `PlantController.ActivePlants` và `PlayerController.Instance` cho phép toàn bộ quái vật trên bản đồ định vị mục tiêu tức thì với tốc độ $O(N)$ và $O(1)$, loại bỏ hoàn toàn các lệnh quét cây phân cấp nặng nề như `FindObjectsByType`.
* **Data-Driven Design:** Cấu hình toàn bộ dữ liệu vật phẩm (`ItemData`), vũ khí (`WeaponData`), cây trồng (`PlanData`), và đợt quái (`WaveData`) qua Unity **ScriptableObject**, dễ dàng cân bằng chỉ số mà không cần sửa code.
* **Object Pooling (`PoolManager`):** Tái sử dụng đối tượng hiệu năng cao cho Quái vật, Đạn/Tên, Đồng xu rơi và Nông sản thu hoạch, triệt tiêu hiện tượng giật khung hình do Garbage Collector.
* **Event-Driven Architecture:** Các component chỉ số (`PlayerHealth`, `PlayerStamina`, `PlayerExperience`, `ShopManager`) phát sự kiện C# thuần túy để UI (`PlayerHUD`, `ShopSlotUI`) lắng nghe và cập nhật, giữ cho logic game độc lập hoàn toàn với Canvas UI.

---

## 3. Hệ Thống Tính Năng Nổi Bật

### 3.1. Cơ Chế Chiến Đấu & Hit & Run
* **Đa dạng vũ khí:**
  * **Cây Giáo (`Spear`):** Vũ khí cận chiến tân thủ với sát thương chuẩn $34\text{ Damage}$ (chuẩn 3 nhát chém hạ gục quái $100\text{ HP}$).
  * **Rìu chiến (`Axe`):** Vũ khí chặt cây thu thập gỗ và chiến đấu diện rộng.
  * **Cung tên (`Bow`):** Tấn công tầm xa, phóng mũi tên theo góc xoay chính xác về hướng ngắm.
  * **Cuốc (`PickaxeAndHoe`):** Khai thác đá và cuốc đất canh tác.
* **Cơ chế Hit & Run hoàn hảo:** Tách biệt hướng di chuyển (`moveInput`) và hướng nhìn ra đòn (`LastInputX`, `LastInputY`). Khi người chơi vừa bấm lùi né quái vừa bắn cung, nhân vật vẫn giữ nguyên góc ngắm chuẩn về phía trước, mũi tên bay đúng mục tiêu mà không bị bẻ lái hay xoay giật Animator Blend Tree.
* **Vật lý đẩy lùi (`KnockbackReceiver`):** Nhân vật và quái vật phản ứng vật lý tự nhiên với lực đẩy lùi từ đòn đánh/bẫy, tự động nhường quyền điều khiển vận tốc cho Rigidbody2D mà không bị giật khựng.

### 3.2. AI Kẻ Địch Thuần 2D (Hybrid A* & Context Steering)
* **Cảm biến né vật cản 2D (Context Steering / Whiskers đa hướng):** Quái vật dùng `Physics2D.CircleCast` kết hợp xòe các góc cảm biến ($\pm 30^\circ, \pm 60^\circ, \pm 90^\circ, \pm 120^\circ, \pm 150^\circ$) để lách qua bờ sông, vách đá mượt mà mà không tốn tài nguyên tìm đường.
* **Mục tiêu 3 cấp không bao giờ đứng im (Triple-Target AI):**
  1. Cây trồng gần nhất (`PlantController` còn sống và hợp lệ).
  2. Người chơi (`PlayerController` còn sống).
  3. Tâm bản đồ (`[MapCenterTarget]` tại `Vector3.zero`).
  * Tự động phục hồi mục tiêu ngay trong frame khi cây bị phá, không bao giờ để quái rơi vào trạng thái bơ vơ.
* **A* Pathfinding 2D Cốt lõi (`PathfindingGrid2D`, `Pathfinding2D`):**
  * Tự động kích hoạt khi bị ngăn cách bởi tường hoặc sông lớn (`HasDirectLineOfSight == false`).
  * Tối ưu hóa **Bounding-Box Overlap Scanning**: Lưới rộng $528\text{m} \times 384\text{m}$ ($203.136\text{ Nodes}$) nạp xong chỉ trong $< 0.1\text{s}$.
  * **Viewport Frustum Culling cho Gizmos:** Chỉ vẽ các ô trong bán kính $25\text{m}$ quanh Camera Scene View, triệt tiêu 100% nguy cơ treo giật Unity Editor.
* **Đợt quái vô tận (Endless Wave Scaling):** Số lượng quái, Máu (`EnemyHealth`) và Sát thương (`EnemyAttackDamage`) tăng tiến tự động theo từng đợt sóng (`Wave`).

### 3.3. Canh Tác Nông Nghiệp & Cây Trồng
* **Chu trình nông nghiệp khép kín:**
  1. Dùng Cuốc cuốc tơi các ô đất trên Tilemap.
  2. Gieo hạt giống (`PlantSeed`) từ túi đồ xuống ô đất đã xới.
  3. Cây sinh trưởng theo thời gian thực qua nhiều giai đoạn trực quan (`PlanData.growthStageSprites`).
  4. Thu hoạch cây chín rụng nông sản qua `PoolManager` và tự động cộng điểm kinh nghiệm (`ExpReward`).
* **Đa dạng cây trồng:** Cà rốt (Carrot), Củ dền (Beetroot), Tỏi (Garlic), Súp lơ (Cauliflower), Bông gòn (Cotton)...

### 3.4. Hệ Thống Bẫy Thủ Thành & Hiệu Ứng Trạng Thái
* **Bẫy chiến thuật:** Người chơi có thể đặt các loại bẫy lên lưới ô bản đồ (`PlayerPlacement`):
  * **Bẫy Lửa (`BurnEffect`):** Đốt quái vật nhận sát thương theo chu kỳ, đổi màu đỏ cam rực lửa.
  * **Bẫy Băng (`FreezeZoneEffect`):** Đóng băng quái vật đứng yên bất động, đổi sắc thái xanh tuyết.
  * **Bẫy Gai / Bẫy Chậm (`SlowEffect`):** Làm chậm tốc độ di chuyển của quái.
* **An toàn tuyệt đối cho người chơi:** Tích hợp cờ bảo vệ `affectPlayer = false`, bẫy chỉ kích hoạt lên quái vật và hoàn toàn vô hại đối với nhân vật người chơi.

### 3.5. Kinh Tế, Cửa Hàng & Túi Đồ
* **Túi đồ 40 ô (Inventory System):** Hỗ trợ kéo-thả hoán đổi vị trí vật phẩm, 10 ô phím nóng nhanh (Hotbar 0-9), sử dụng thức ăn để hồi phục Máu và Thể lực.
* **Cửa Hàng Tối Giản (`ShopManager`):**
  * Giao diện cuộn 2 cột trực quan gồm 4 phân khu: Hạt giống, Nông sản, Bẫy, Vũ khí.
  * **Cơ chế lạm phát giá bẫy (Escalating Pricing):** Giá bẫy tự động tăng thêm $+10\text{ Coin}$ sau mỗi lần mua để cân bằng chiến thuật thủ thành.
  * **Vũ khí mua 1 lần:** Sau khi mua vũ khí, ô vật phẩm tự động ẩn trong Cửa Hàng và chỉ hiển thị lại khi người chơi bán vũ khí đó đi.
* **Kinh tế cân bằng:** Quái vật rớt vàng ($8 - 10\text{ Coin/con}$ ở Wave 1), hỗ trợ chặt cây bán gỗ ($2\text{ Coin/khúc}$) và đập đá bán đá ($4\text{ Coin/viên}$).

### 3.6. Hệ Thống Lưu / Tải Game (Save & Load Multi-Slot)
* **3 Slot lưu game độc lập:** Lưu trữ trạng thái thế giới dưới dạng JSON an toàn (`save_slot_1.json`, `save_slot_2.json`, `save_slot_3.json`).
* **Kiến trúc DTO sạch (`GameSaveData`):**
  * Lưu trữ trọn vẹn: Vị trí người chơi, Máu, Thể lực, Thức ăn, Cấp độ, Kinh nghiệm, Toàn bộ 34 ô đồ, Vàng, Số lượng mua lạm phát trong Shop.
  * Lưu trữ thế giới: Toàn bộ ô đất đã xới, danh sách cây trồng (giai đoạn lớn, thời gian), bẫy đã đặt, và Wave quái vật hiện tại.
* **Khôi phục sạch sẽ (`CleanupWorldState`):** Tự động dọn sạch quái vật, cây trồng, vật phẩm rơi cũ trước khi nạp dữ liệu mới, ngăn ngừa triệt để lỗi nhân đôi thực thể.

---

## 4. Phím Tắt Điều Khiển (Controls)

| Phím | Hành Động | Ghi Chú |
| :--- | :--- | :--- |
| `W`, `A`, `S`, `D` / `Phím Mũi Tên` | **Di chuyển (Move)** | 8 hướng mượt mà, tự động lật Sprite |
| `Shift Trái` | **Chạy nhanh (Sprint)** | Tiêu hao Thể lực (Stamina) theo thời gian |
| `Phím Cách (Space)` / `Click Chuột Trái` | **Tấn công / Sử dụng (Action)** | Chém vũ khí, bắn cung, cuốc đất, ăn thức ăn |
| `Tab` | **Đóng / Mở Túi đồ (Inventory)** | Bật mở giao diện túi đồ và cửa hàng |
| `0` - `9` hoặc `Lăn chuột` | **Chọn ô phím nóng (Hotbar)** | Chọn nhanh trang bị/hạt giống trên tay |
| `ESC` | **Menu Tạm dừng / Cài đặt** | Tạm dừng thế giới, chỉnh âm lượng BGM/SFX, Lưu game |

---

## 5. Cấu Trúc Thư Mục Dự Án

```text
Assets/
├── Animation/                    # Hoạt ảnh Nhân vật, Quái vật, Bẫy, Vũ khí
├── Data/                         # ScriptableObjects dữ liệu (ItemData, WeaponData, WaveData...)
├── Editor/                       # Công cụ hỗ trợ Editor (Setup A*, PathfindingGrid2DEditor)
├── Prefabs/                      # Prefab mẫu (Player, Enemies, Traps, Crops, Coins, UI...)
├── Scenes/                       # Scene màn chơi chính (CoreScence.unity)
├── Script/                       # Toàn bộ mã nguồn C# của dự án
│   ├── BaseAttribute/            # Chỉ số kế thừa cốt lõi (BaseHealth, BaseDamage, BaseSpeed, BaseStamina)
│   ├── EffectReceiver/           # Nhận hiệu ứng trạng thái (BurnReceiver, FreezeReceiver, KnockbackReceiver...)
│   ├── Entity/
│   │   ├── Enemy/                # AI Quái vật, Spawn, Tầm nhìn (EnemyController, EnemyVision, EnemySpawn)
│   │   ├── Plant/                # Cây trồng nông nghiệp (PlantController, PlantSeed)
│   │   ├── Player/               # Phân hệ Người chơi (PlayerController, PlayerFarming, PlayerEquipment...)
│   │   ├── Resource/             # Cây cối, Mỏ đá tài nguyên (ResourceNode, BaseDrop)
│   │   ├── Trap/                 # Bẫy chiến thuật (BurnEffect, FreezeZoneEffect, SlowEffect)
│   │   └── Weapon/               # Vũ khí chiến đấu (BowWeapon, MeleeWeapon, HoeWeapon...)
│   ├── Manager/                  # Các bộ quản lý trung tâm (SaveManager, ShopManager, AudioManager, PoolManager...)
│   ├── Map/                      # Vùng sương mù, mở khóa khu vực (ZoneArea, ZoneFogManager...)
│   ├── Pathfinding/              # Thuật toán tìm đường A* 2D (PathNode2D, PathfindingGrid2D, Pathfinding2D)
│   └── UI/                       # Giao diện người dùng (PlayerHUD, ShopSlotUI, SaveSlotUIController...)
└── Sprites/                      # Tài nguyên hình ảnh 2D, Tileset, Icon, Nông sản
```

---

## 6. Yêu Cầu Cấu Hình & Cài Đặt

### Yêu Cầu Môi Trường
* **Unity Version:** Unity 6 (6000.0.x trở lên)
* **Render Pipeline:** Universal Render Pipeline (URP) hoặc 2D Built-in Render Pipeline
* **Packages Bắt Buộc:**
  * `com.unity.inputsystem` (Unity New Input System)
  * `com.unity.2d.tilemap` (2D Tilemap Editor)
  * `com.unity.ugui` (TextMeshPro & UI)

### Hướng Dẫn Chạy Dự Án
1. Mở **Unity Hub** và chọn `Add project from disk`.
2. Trỏ đến thư mục dự án `knight`.
3. Mở dự án bằng phiên bản **Unity 6**.
4. Trong cửa sổ **Project**, truy cập đường dẫn:
   `Assets/Scenes/CoreScence.unity`
5. Nhấp đúp chuột để mở Scene.
6. Nhấn nút **Play ▶** trên thanh công cụ Unity để bắt đầu trải nghiệm game từ màn hình Menu Chính!

---

## 7. Tác Giả

* **Đoàn Hoàng** - *Nhà phát triển chính & Thiết kế kiến trúc hệ thống*.
* Dự án phục vụ mục đích nghiên cứu chuyên sâu về kiến trúc Clean Code trong Unity, mô hình Orchestrator Pattern, thuật toán AI 2D A* và cơ chế cân bằng kinh tế game sinh tồn.
