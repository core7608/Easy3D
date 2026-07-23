# 🎨 Easy3D - برنامج التصميم ثلاثي الأبعاد

<div align="center">

![Easy3D Logo](docs/logo.png)

**برنامج تصميم ثلاثي الأبعاد متقدم يعمل على جميع المنصات بدون كارت شاشة**

[![Build](https://github.com/yourusername/Easy3D/actions/workflows/build.yml/badge.svg)](https://github.com/yourusername/Easy3D/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com)

</div>

---

## ✨ المميزات

### 🎯 سهولة الاستخدام
- واجهة عربية بالكامل
- اختصارات متطابقة مع Blender
- أدوات بديهية للمبتدئين

### 🚀 أداء عالي بدون GPU
- محرك رسم برمجي (Software Rendering)
- يعمل على أي جهاز بدون كرت شاشة
- استهلاك موارد منخفض

### 🎨 رندر متقدم
- **PBR** (Physically Based Rendering)
- **Phong / Blinn-Phong**
- **Toon Shading** (كرتوني)
- **Flat / Unlit**
- إضاءة اتجاهية ونقطية ومخروطية
- ظلال وانعكاسات

### 🖼️ تكستشرات ومواد
- تحميل صور (PNG, JPG, BMP, TGA)
- تكستشرات: Diffuse, Normal, Specular, Emissive
- فلاتر: Blur, Sharpen, Emboss, Edge Detect
- مواد جاهزة: معدن، زجاج، خشب، مطاط، قماش

### 📁 تصدير واستيراد
| الصيغة | التصدير | الاستيراد |
|--------|---------|-----------|
| **OBJ** | ✅ | ✅ |
| **STL** | ✅ | ✅ |
| **GLTF/GLB** | ✅ | ✅ |
| **Three.js JSON** | ✅ | - |
| **FBX** | ✅ | - |
| **PLY** | ✅ | ✅ |
| **COLLADA** | ✅ | - |
| **X3D** | ✅ | - |
| **VRML** | ✅ | - |
| **USD** | ✅ | - |
| **AMF** | ✅ | - |
| **OFF** | ✅ | - |
| **CSV** | ✅ | - |

### 🌍 منصات متعددة
- 🪟 Windows (x64, ARM64)
- 🐧 Linux (x64, ARM64)
- 🍎 macOS (x64, Apple Silicon)

---

## 📦 التثبيت

### من GitHub Releases
1. اذهب إلى [Releases](https://github.com/yourusername/Easy3D/releases)
2. حمل الإصدار المناسب لمنصتك
3. شغّل الملف

### من المصدر
```bash
# استنساخ المستودع
git clone https://github.com/yourusername/Easy3D.git
cd Easy3D

# بناء وتشغيل
dotnet run --project src/Easy3D
```

### بناء أحادي الملف
```bash
# Windows
dotnet publish src/Easy3D -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux
dotnet publish src/Easy3D -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS
dotnet publish src/Easy3D -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 🎮 الاختصارات

### الأدوات
| الاختصار | الوظيفة |
|----------|---------|
| `W` | اختيار |
| `G` | تحريك |
| `R` | تدوير |
| `S` | تحجيم |
| `F` | التركيز على المحدد |

### الكاميرا
| الاختصار | الوظيفة |
|----------|---------|
| `عجلة الماوس` | تقريب/تبعيد |
| `الزر الأوسط` | دوران |
| `الزر الأيمن` | تحريك |
| `Numpad 1` | أمامي |
| `Numpad 3` | يمين |
| `Numpad 7` | علوي |

### العمليات
| الاختصار | الوظيفة |
|----------|---------|
| `Ctrl+N` | مشروع جديد |
| `Ctrl+O` | فتح |
| `Ctrl+S` | حفظ |
| `Ctrl+D` | تكرار |
| `Delete` | حذف |
| `Ctrl+Z` | تراجع |
| `Ctrl+Y` | إعادة |

---

## 🏗️ هيكل المشروع

```
Easy3D/
├── src/
│   ├── Easy3D.Core/          # المحرك الأساسي
│   │   ├── Vector3.cs        # متجهات
│   │   ├── Matrix4x4.cs      # مصفوفات
│   │   ├── Color.cs          # ألوان
│   │   ├── Texture.cs        # تكستشرات
│   │   ├── Material.cs       # مواد PBR
│   │   ├── Camera.cs         # كاميرا
│   │   ├── Light.cs          # إضاءة
│   │   └── Scene.cs          # مشهد
│   ├── Easy3D.Geometry/      # الأشكال
│   │   ├── Mesh.cs           # شبكة
│   │   └── MeshFactory.cs    # مصنع الأشكال
│   ├── Easy3D.Rendering/     # الرسم
│   │   └── SoftwareRenderer.cs
│   ├── Easy3D.Exporters/     # التصدير
│   │   └── ExportManager.cs
│   └── Easy3D/               # الواجهة
│       ├── App.axaml
│       └── UI/Views/
│           └── MainWindow.axaml
├── .github/workflows/        # CI/CD
├── build.sh                  # سكريبت بناء
└── README.md
```

---

## 🛠️ المتطلبات

- **.NET 8.0 SDK** أو أحدث
- لا يحتاج كرت شاشة
- يعمل على任何 جهاز

---

## 📊 مقارنة مع Blender

| الميزة | Easy3D | Blender |
|--------|--------|---------|
| **اللغة** | C# | C++ |
| **الواجهة** | Avalonia | SDL/OpenGL |
| **الرسم** | Software | OpenGL/Vulkan |
| **GPU** | ❌ لا يحتاج | ✅ مطلوب |
| **الحجم** | ~50 MB | ~300 MB |
| **المنصات** | Win/Linux/Mac | Win/Linux/Mac |
| **سهولة التعلم** | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **التصدير** | 15 صيغة | 20+ صيغة |

---

## 🤝 المساهمة

المساهمات مرحب بها! يرجى قراءة [CONTRIBUTING.md](CONTRIBUTING.md) أولاً.

---

## 📄 الرخصة

هذا المشروع مرخص تحت رخصة MIT - انظر [LICENSE](LICENSE)

---

<div align="center">

**Easy3D** - تصميم ثلاثي الأبعاد للجميع! 🎨

</div>
