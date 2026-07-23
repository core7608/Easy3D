# Contributing to Easy3D

أهلاً بك! نحن سعداء بمساهمتك في مشروع Easy3D.

## كيفية المساهمة

### الإبلاغ عن bugs
1. افتح [Issue](https://github.com/yourusername/Easy3D/issues)
2. اختر "Bug Report"
3. املأ النموذج بالمعلومات المطلوبة

### اقتراح ميزات
1. افتح [Issue](https://github.com/yourusername/Easy3D/issues)
2. اختر "Feature Request"
3. اشرح الفكرة بالتفصيل

### إرسال Pull Requests

1. ** Fork المشروع **
```bash
git clone https://github.com/YOUR_USERNAME/Easy3D.git
cd Easy3D
git checkout -b feature/my-feature
```

2. **SetBranch **
```bash
git checkout -b feature/amazing-feature
# أو
git checkout -b fix/bug-fix
```

3. ** الكود **
- اتبع معايير الكود الموجودة
- أضف تعليقات للكود المعقد
- تأكد من أن الكود يعمل بدون أخطاء

4. ** الاختبار **
```bash
dotnet test src/Easy3D.sln
```

5. ** Commit **
```bash
git add .
git commit -m "feat: add amazing feature"
```

6. ** Push و PR **
```bash
git push origin feature/amazing-feature
```
ثم افتح Pull Request على GitHub.

## معايير الكود

### C# Coding Standards
- استخدم `PascalCase` للأسماء العامة
- استخدم `camelCase` للمتغيرات المحلية
- استخدم `_camelCase` للحقول الخاصة
- أضف `async` للدوال غير المتزامنة

### أمثلة
```csharp
// ✅ صحيح
public class MeshFactory
{
    private readonly List<Vertex> _vertices;
    
    public Mesh CreateCube(float size = 1f)
    {
        // ...
    }
}

// ❌ خطأ
public class mesh_factory
{
    public List<Vertex> Vertices;
    
    public mesh Create_cube(float Size)
    {
        // ...
    }
}
```

### Commit Messages
استخدم [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add new feature
fix: fix bug
docs: update documentation
style: format code
refactor: refactor code
test: add tests
chore: update dependencies
```

## هيكل المشروع

```
Easy3D/
├── src/
│   ├── Easy3D.Core/      # المحرك الأساسي
│   ├── Easy3D.Geometry/  # الأشكال الهندسية
│   ├── Easy3D.Rendering/ # محرك الرسم
│   ├── Easy3D.Exporters/ # التصدير والاستيراد
│   └── Easy3D/           # واجهة المستخدم
```

## الأسئلة الشائعة

### كيف أضيف شكل جديد؟
1. أضف الدالة في `MeshFactory.cs`
2. أضف الزر في `MainWindow.axaml`
3. أضف الأمر في `MainWindow.axaml.cs`

### كيف أضيف صيغة تصدير جديدة؟
1. أضف الصيغة في `ExportFormat` enum
2. أضف الدالة في `ExportManager.cs`
3. أضف الخيار في القائمة

## التواصل

- ** Issues **: لل bugs والميزات
- ** Discussions **: للأسئلة العامة
- ** Email **: your@email.com

## الرخصة

بمساهمتك، توافق على أن الكود سيتم ترخيصه تحت رخصة MIT.

## شكر خاص

شكراً لجميع المساهمين! 🎉

[![Contributors](https://contrib.rocks/image?repo=yourusername/Easy3D)](https://github.com/yourusername/Easy3D/graphs/contributors)
