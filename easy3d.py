"""
Easy3D - برنامج تصميم ثلاثي الأبعاد بسيط وخفيف
يعمل بدون كارت شاشة - مناسب للأجهزة الضعيفة
"""

import tkinter as tk
from tkinter import ttk, messagebox, filedialog, colorchooser
import math
import json
import os

# ==================== المحرك ثلاثي الأبعاد ====================

class Vector3:
    """فئة للتعامل مع النقاط والاتجاهات ثلاثية الأبعاد"""
    def __init__(self, x=0, y=0, z=0):
        self.x = x
        self.y = y
        self.z = z
    
    def copy(self):
        return Vector3(self.x, self.y, self.z)
    
    def to_tuple(self):
        return (self.x, self.y, self.z)
    
    def __add__(self, other):
        return Vector3(self.x + other.x, self.y + other.y, self.z + other.z)
    
    def __sub__(self, other):
        return Vector3(self.x - other.x, self.y - other.y, self.z - other.z)
    
    def __mul__(self, scalar):
        return Vector3(self.x * scalar, self.y * scalar, self.z * scalar)
    
    def length(self):
        return math.sqrt(self.x**2 + self.y**2 + self.z**2)
    
    def normalize(self):
        l = self.length()
        if l > 0:
            return Vector3(self.x/l, self.y/l, self.z/l)
        return Vector3(0, 0, 0)
    
    def dot(self, other):
        return self.x * other.x + self.y * other.y + self.z * other.z
    
    def cross(self, other):
        return Vector3(
            self.y * other.z - self.z * other.y,
            self.z * other.x - self.x * other.z,
            self.x * other.y - self.y * other.x
        )


def rotate_point(point, rx, ry, rz):
    """تدوير نقطة حول المحاور الثلاثة"""
    # تدوير حول X
    cos_x, sin_x = math.cos(rx), math.sin(rx)
    y = point.y * cos_x - point.z * sin_x
    z = point.y * sin_x + point.z * cos_x
    point = Vector3(point.x, y, z)
    
    # تدوير حول Y
    cos_y, sin_y = math.cos(ry), math.sin(ry)
    x = point.x * cos_y + point.z * sin_y
    z = -point.x * sin_y + point.z * cos_y
    point = Vector3(x, point.y, z)
    
    # تدوير حول Z
    cos_z, sin_z = math.cos(rz), math.sin(rz)
    x = point.x * cos_z - point.y * sin_z
    y = point.x * sin_z + point.y * cos_z
    
    return Vector3(x, y, z)


class Material:
    """مادة العنصر"""
    def __init__(self, color="#4a90d9", metalness=0, roughness=0.5, opacity=1.0):
        self.color = color
        self.metalness = metalness
        self.roughness = roughness
        self.opacity = opacity
    
    def to_dict(self):
        return {
            "color": self.color,
            "metalness": self.metalness,
            "roughness": self.roughness,
            "opacity": self.opacity
        }
    
    @staticmethod
    def from_dict(d):
        return Material(d["color"], d["metalness"], d["roughness"], d["opacity"])


class Mesh3D:
    """شبكة ثلاثية الأبعاد (كائن)"""
    def __init__(self, name="كائن", vertices=None, faces=None):
        self.name = name
        self.vertices = vertices or []  # قائمة النقاط
        self.faces = faces or []  # قائمة الأوجه
        self.position = Vector3(0, 0, 0)
        self.rotation = Vector3(0, 0, 0)  # بالراديان
        self.scale = Vector3(1, 1, 1)
        self.material = Material()
        self.visible = True
        self.selected = False
    
    def get_world_vertices(self):
        """الحصول على النقاط بعد التحويلات"""
        result = []
        for v in self.vertices:
            # تحجيم
            sv = Vector3(v.x * self.scale.x, v.y * self.scale.y, v.z * self.scale.z)
            # تدوير
            rv = rotate_point(sv, self.rotation.x, self.rotation.y, self.rotation.z)
            # نقل
            wv = rv + self.position
            result.append(wv)
        return result
    
    def get_face_center(self, face, world_verts):
        """مركز الوجه"""
        cx, cy = 0, 0, 0
        for idx in face:
            v = world_verts[idx]
            cx += v.x
            cy += v.y
        cx /= len(face)
        cy /= len(face)
        return cx, cy
    
    def get_face_normal(self, face, world_verts):
        """حساب العمودي للوجه"""
        if len(face) < 3:
            return Vector3(0, 0, 1)
        v0 = world_verts[face[0]]
        v1 = world_verts[face[1]]
        v2 = world_verts[face[2]]
        e1 = v1 - v0
        e2 = v2 - v0
        return e1.cross(e2).normalize()
    
    def to_dict(self):
        return {
            "name": self.name,
            "vertices": [(v.x, v.y, v.z) for v in self.vertices],
            "faces": self.faces,
            "position": self.position.to_tuple(),
            "rotation": (math.degrees(self.rotation.x), math.degrees(self.rotation.y), math.degrees(self.rotation.z)),
            "scale": self.scale.to_tuple(),
            "material": self.material.to_dict()
        }
    
    @staticmethod
    def from_dict(d):
        mesh = Mesh3D(d["name"])
        mesh.vertices = [Vector3(*v) for v in d["vertices"]]
        mesh.faces = d["faces"]
        mesh.position = Vector3(*d["position"])
        mesh.rotation = Vector3(math.radians(d["rotation"][0]), math.radians(d["rotation"][1]), math.radians(d["rotation"][2]))
        mesh.scale = Vector3(*d["scale"])
        mesh.material = Material.from_dict(d["material"])
        return mesh


def create_cube(size=1):
    """إنشاء مكعب"""
    s = size / 2
    vertices = [
        Vector3(-s, -s, -s), Vector3(s, -s, -s), Vector3(s, s, -s), Vector3(-s, s, -s),
        Vector3(-s, -s, s), Vector3(s, -s, s), Vector3(s, s, s), Vector3(-s, s, s),
    ]
    faces = [
        [0, 1, 2, 3],  # أمامي
        [5, 4, 7, 6],  # خلفي
        [4, 0, 3, 7],  # يسار
        [1, 5, 6, 2],  # يمين
        [3, 2, 6, 7],  # علوي
        [4, 5, 1, 0],  # سفلي
    ]
    return Mesh3D("مكعب", vertices, faces)


def create_sphere(radius=0.5, segments=12):
    """إنشاء كرة"""
    vertices = []
    faces = []
    
    for i in range(segments + 1):
        lat = math.pi * i / segments
        for j in range(segments):
            lon = 2 * math.pi * j / segments
            x = radius * math.sin(lat) * math.cos(lon)
            y = radius * math.cos(lat)
            z = radius * math.sin(lat) * math.sin(lon)
            vertices.append(Vector3(x, y, z))
    
    for i in range(segments):
        for j in range(segments):
            p1 = i * segments + j
            p2 = i * segments + (j + 1) % segments
            p3 = (i + 1) * segments + (j + 1) % segments
            p4 = (i + 1) * segments + j
            faces.append([p1, p2, p3, p4])
    
    return Mesh3D("كرة", vertices, faces)


def create_cylinder(radius=0.5, height=1, segments=12):
    """إنشاء أسطوانة"""
    vertices = []
    faces = []
    
    # القمة والقاع
    vertices.append(Vector3(0, height/2, 0))  # 0
    vertices.append(Vector3(0, -height/2, 0))  # 1
    
    # الحواف
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        vertices.append(Vector3(x, height/2, z))  # top
        vertices.append(Vector3(x, -height/2, z))  # bottom
    
    # الأوجه الجانبية
    for i in range(segments):
        t1 = 2 + i * 2
        b1 = 3 + i * 2
        t2 = 2 + ((i + 1) % segments) * 2
        b2 = 3 + ((i + 1) % segments) * 2
        faces.append([t1, b1, b2, t2])
    
    # الأوجه العلوية والسفلية
    top_face = [2 + i * 2 for i in range(segments)]
    bottom_face = [3 + i * 2 for i in range(segments - 1, -1, -1)]
    faces.append(top_face)
    faces.append(bottom_face)
    
    return Mesh3D("أسطوانة", vertices, faces)


def create_cone(radius=0.5, height=1, segments=12):
    """إنشاء مخروط"""
    vertices = []
    faces = []
    
    vertices.append(Vector3(0, height/2, 0))  # القمة
    vertices.append(Vector3(0, -height/2, 0))  # المركز السفلي
    
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        vertices.append(Vector3(x, -height/2, z))
    
    # الأوجه الجانبية
    for i in range(segments):
        faces.append([0, 2 + i, 2 + (i + 1) % segments])
    
    # الوجه السفلي
    bottom_face = [2 + i for i in range(segments - 1, -1, -1)]
    faces.append(bottom_face)
    
    return Mesh3D("مخروط", vertices, faces)


def create_plane(size=1):
    """إنشاء مستوي"""
    s = size / 2
    vertices = [
        Vector3(-s, 0, -s), Vector3(s, 0, -s),
        Vector3(s, 0, s), Vector3(-s, 0, s),
    ]
    faces = [[0, 1, 2, 3]]
    return Mesh3D("مستوي", vertices, faces)


def create_torus(radius=0.5, tube=0.15, segments=12, tube_segments=8):
    """إنشاء حلقة"""
    vertices = []
    faces = []
    
    for i in range(segments):
        theta = 2 * math.pi * i / segments
        for j in range(tube_segments):
            phi = 2 * math.pi * j / tube_segments
            x = (radius + tube * math.cos(phi)) * math.cos(theta)
            y = tube * math.sin(phi)
            z = (radius + tube * math.cos(phi)) * math.sin(theta)
            vertices.append(Vector3(x, y, z))
    
    for i in range(segments):
        for j in range(tube_segments):
            p1 = i * tube_segments + j
            p2 = i * tube_segments + (j + 1) % tube_segments
            p3 = ((i + 1) % segments) * tube_segments + (j + 1) % tube_segments
            p4 = ((i + 1) % segments) * tube_segments + j
            faces.append([p1, p2, p3, p4])
    
    return Mesh3D("حلقة", vertices, faces)


# ==================== نظام الرسم ====================

class SoftwareRenderer:
    """محرك رسم برمجي (لا يحتاج كارت شاشة)"""
    
    def __init__(self, canvas, width, height):
        self.canvas = canvas
        self.width = width
        self.height = height
        self.camera_pos = Vector3(3, 3, 3)
        self.camera_target = Vector3(0, 0, 0)
        self.camera_up = Vector3(0, 1, 0)
        self.fov = 60
        self.near = 0.1
        self.far = 100
        self.light_dir = Vector3(-1, -1, -1).normalize()
        self.bg_color = "#1a1a2e"
        self.grid_color = "#2a2a4a"
        
        # مخزن الصورة
        self.pixels = {}
        self.z_buffer = {}
    
    def clear(self):
        """مسح الشاشة"""
        self.canvas.delete("all")
        self.canvas.create_rectangle(0, 0, self.width, self.height, fill=self.bg_color, outline="")
        self.z_buffer = {}
    
    def project_point(self, point):
        """إسقاط نقطة ثلاثية الأبعاد على الشاشة"""
        # حساب المتجه بالنسبة للكاميرا
        forward = (self.camera_target - self.camera_pos).normalize()
        right = forward.cross(self.camera_up).normalize()
        up = right.cross(forward).normalize()
        
        # تحويل لنظام الكاميرا
        p = point - self.camera_pos
        x = p.dot(right)
        y = p.dot(up)
        z = p.dot(forward)
        
        if z < self.near:
            return None
        
        # إسقاط المنظور
        aspect = self.width / self.height
        fov_rad = math.radians(self.fov)
        f = 1 / math.tan(fov_rad / 2)
        
        px = (x * f / aspect) / z
        py = (y * f) / z
        
        # تحويل لتنسيق الشاشة
        sx = (px + 1) * self.width / 2
        sy = (1 - py) * self.height / 2
        
        return (sx, sy, z)
    
    def draw_line(self, p1, p2, color="#ffffff", width=1):
        """رسم خط"""
        self.canvas.create_line(p1[0], p1[1], p2[0], p2[1], 
                               fill=color, width=width)
    
    def draw_polygon(self, points, fill_color, outline_color=None):
        """رسم مضلع"""
        if len(points) < 3:
            return
        
        coords = []
        for p in points:
            coords.extend([p[0], p[1]])
        
        if outline_color is None:
            outline_color = self.adjust_color(fill_color, 0.7)
        
        self.canvas.create_polygon(coords, fill=fill_color, outline=outline_color, width=1)
    
    def adjust_color(self, color, factor):
        """تعديل لون"""
        try:
            r = int(color[1:3], 16)
            g = int(color[3:5], 16)
            b = int(color[5:7], 16)
            r = min(255, int(r * factor))
            g = min(255, int(g * factor))
            b = min(255, int(b * factor))
            return f"#{r:02x}{g:02x}{b:02x}"
        except:
            return color
    
    def calculate_lighting(self, normal, base_color):
        """حساب الإضاءة"""
        # إضاءة محيطية
        ambient = 0.3
        
        # إضاءة انتشارية
        diff = max(0, normal.dot(self.light_dir))
        
        # إضاءة خاصة (блиج)
        view_dir = (self.camera_pos - self.camera_target).normalize()
        reflect = self.light_dir - normal * (2 * normal.dot(self.light_dir))
        spec = max(0, view_dir.dot(reflect)) ** 32
        
        intensity = min(1, ambient + diff * 0.6 + spec * 0.1)
        
        try:
            r = int(base_color[1:3], 16)
            g = int(base_color[3:5], 16)
            b = int(base_color[5:7], 16)
            r = min(255, int(r * intensity))
            g = min(255, int(g * intensity))
            b = min(255, int(b * intensity))
            return f"#{r:02x}{g:02x}{b:02x}"
        except:
            return base_color
    
    def draw_grid(self, size=10, divisions=20):
        """رسم الشبكة"""
        step = size / divisions
        half = size / 2
        
        for i in range(divisions + 1):
            x = -half + i * step
            p1 = self.project_point(Vector3(x, 0, -half))
            p2 = self.project_point(Vector3(x, 0, half))
            if p1 and p2:
                self.draw_line(p1, p2, self.grid_color, 0.5)
            
            p1 = self.project_point(Vector3(-half, 0, x))
            p2 = self.project_point(Vector3(half, 0, x))
            if p1 and p2:
                self.draw_line(p1, p2, self.grid_color, 0.5)
        
        # المحاور
        origin = self.project_point(Vector3(0, 0, 0))
        if origin:
            # محور X - أحمر
            px = self.project_point(Vector3(2, 0, 0))
            if px:
                self.draw_line(origin, px, "#ff4444", 2)
            
            # محور Y - أخضر
            py = self.project_point(Vector3(0, 2, 0))
            if py:
                self.draw_line(origin, py, "#44ff44", 2)
            
            # محور Z - أزرق
            pz = self.project_point(Vector3(0, 0, 2))
            if pz:
                self.draw_line(origin, pz, "#4444ff", 2)
    
    def draw_mesh(self, mesh):
        """رسم شبكة ثلاثية الأبعاد"""
        if not mesh.visible:
            return
        
        world_verts = mesh.get_world_vertices()
        projected = []
        
        for v in world_verts:
            p = self.project_point(v)
            projected.append(p)
        
        if not projected:
            return
        
        # ترتيب الأوجه حسب العمق (back-face culling)
        faces_with_depth = []
        for face in mesh.faces:
            verts_3d = [world_verts[i] for i in face if i < len(world_verts)]
            if len(verts_3d) < 3:
                continue
            
            # حساب العمق المتوسط
            avg_z = sum(v.dot((self.camera_pos - self.camera_target).normalize()) for v in verts_3d) / len(verts_3d)
            
            # حساب العمودي
            normal = mesh.get_face_normal(face, world_verts)
            
            # فلترة الأوجه الخلفية
            view_dir = (self.camera_pos - self.camera_target).normalize()
            if normal.dot(view_dir) > 0.1:
                continue
            
            faces_with_depth.append((avg_z, face, normal))
        
        # ترتيب من الأبعد للأقرب
        faces_with_depth.sort(key=lambda x: x[0], reverse=True)
        
        # رسم الأوجه
        for depth, face, normal in faces_with_depth:
            face_points = []
            valid = True
            for idx in face:
                if idx < len(projected) and projected[idx]:
                    face_points.append(projected[idx])
                else:
                    valid = False
                    break
            
            if not valid or len(face_points) < 3:
                continue
            
            # حساب اللون مع الإضاءة
            color = self.calculate_lighting(normal, mesh.material.color)
            
            # رسم الوجه
            self.draw_polygon(face_points, color)
            
            # رسم الحدود
            if mesh.selected:
                for i in range(len(face_points)):
                    p1 = face_points[i]
                    p2 = face_points[(i + 1) % len(face_points)]
                    self.draw_line(p1, p2, "#ffff00", 2)
    
    def render(self, objects):
        """رسم المشهد الكامل"""
        self.clear()
        self.draw_grid()
        
        # ترتيب الكائنات حسب العمق
        cam_dir = (self.camera_pos - self.camera_target).normalize()
        sorted_objects = sorted(objects, 
            key=lambda m: (m.position - self.camera_pos).dot(cam_dir), 
            reverse=True)
        
        for mesh in sorted_objects:
            self.draw_mesh(mesh)
    
    def set_camera_preset(self, view):
        """تعيين زاوية كاميرا محددة"""
        if view == "front":
            self.camera_pos = Vector3(0, 0, 5)
        elif view == "back":
            self.camera_pos = Vector3(0, 0, -5)
        elif view == "right":
            self.camera_pos = Vector3(5, 0, 0)
        elif view == "left":
            self.camera_pos = Vector3(-5, 0, 0)
        elif view == "top":
            self.camera_pos = Vector3(0, 5, 0.01)
        elif view == "bottom":
            self.camera_pos = Vector3(0, -5, 0.01)
        self.camera_target = Vector3(0, 0, 0)


# ==================== الواجهة الرسومية ====================

class Easy3DApp:
    """التطبيق الرئيسي"""
    
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Easy3D - برنامج التصميم ثلاثي الأبعاد")
        self.root.geometry("1400x900")
        self.root.minsize(1000, 700)
        
        # تعيين الألوان
        self.colors = {
            "bg": "#1e1e2e",
            "panel": "#252540",
            "accent": "#4a90d9",
            "text": "#ffffff",
            "text_dim": "#8888aa",
            "border": "#3a3a5a",
            "selected": "#5a9ae9",
            "hover": "#3a3a6a"
        }
        
        self.root.configure(bg=self.colors["bg"])
        
        # المشهد
        self.objects = []
        self.selected_object = None
        self.history = []
        self.history_index = -1
        
        # حالة التفاعل
        self.current_tool = "select"
        self.mode = "object"
        self.dragging = False
        self.drag_start = None
        self.orbiting = False
        self.orbit_start = None
        
        # إنشاء الواجهة
        self.create_styles()
        self.create_menu()
        self.create_toolbar()
        self.create_main_area()
        self.create_properties_panel()
        self.create_status_bar()
        
        # إنشاء المحرك
        self.renderer = SoftwareRenderer(self.canvas, 800, 600)
        
        # ربط الأحداث
        self.bind_events()
        
        # إضافة كائن افتراضي
        self.add_cube()
        
        # بدء الرسم
        self.update_viewport()
    
    def create_styles(self):
        """إنشاء أنماط العناصر"""
        self.style = ttk.Style()
        self.style.theme_use('clam')
        
        self.style.configure("TFrame", background=self.colors["bg"])
        self.style.configure("TLabel", background=self.colors["bg"], foreground=self.colors["text"])
        self.style.configure("TButton", background=self.colors["panel"], foreground=self.colors["text"])
        self.style.configure("Accent.TButton", background=self.colors["accent"], foreground=self.colors["text"])
    
    def create_menu(self):
        """إنشاء شريط القوائم"""
        menubar = tk.Menu(self.root)
        self.root.config(menu=menubar)
        
        # قائمة ملف
        file_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="ملف", menu=file_menu)
        file_menu.add_command(label="جديد", command=self.new_project, accelerator="Ctrl+N")
        file_menu.add_command(label="فتح", command=self.load_project, accelerator="Ctrl+O")
        file_menu.add_command(label="حفظ", command=self.save_project, accelerator="Ctrl+S")
        file_menu.add_separator()
        file_menu.add_command(label="تصدير OBJ", command=self.export_obj)
        file_menu.add_separator()
        file_menu.add_command(label="خروج", command=self.root.quit)
        
        # قائمة تحرير
        edit_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="تحرير", menu=edit_menu)
        edit_menu.add_command(label="تراجع", command=self.undo, accelerator="Ctrl+Z")
        edit_menu.add_command(label="إعادة", command=self.redo, accelerator="Ctrl+Y")
        edit_menu.add_separator()
        edit_menu.add_command(label="تكرار", command=self.duplicate_object, accelerator="Shift+D")
        edit_menu.add_command(label="حذف", command=self.delete_object, accelerator="Delete")
        
        # قائمة إضافة
        add_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="إضافة", menu=add_menu)
        add_menu.add_command(label="مكعب", command=self.add_cube)
        add_menu.add_command(label="كرة", command=self.add_sphere)
        add_menu.add_command(label="أسطوانة", command=self.add_cylinder)
        add_menu.add_command(label="مخروط", command=self.add_cone)
        add_menu.add_command(label="مستوي", command=self.add_plane)
        add_menu.add_command(label="حلقة", command=self.add_torus)
        
        # قائمة عرض
        view_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="عرض", menu=view_menu)
        view_menu.add_command(label="أمامي (Numpad 1)", command=lambda: self.set_view("front"))
        view_menu.add_command(label="خلفي (Numpad Ctrl+1)", command=lambda: self.set_view("back"))
        view_menu.add_command(label="يمين (Numpad 3)", command=lambda: self.set_view("right"))
        view_menu.add_command(label="يسار (Numpad Ctrl+3)", command=lambda: self.set_view("left"))
        view_menu.add_command(label="علوي (Numpad 7)", command=lambda: self.set_view("top"))
        view_menu.add_command(label="سفلي (Numpad Ctrl+7)", command=lambda: self.set_view("bottom"))
        view_menu.add_separator()
        view_menu.add_command(label="إعادة تعيين الكاميرا", command=self.reset_camera)
        
        # قائمة مساعدة
        help_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="مساعدة", menu=help_menu)
        help_menu.add_command(label="الاختصارات", command=self.show_shortcuts)
        help_menu.add_command(label="حول البرنامج", command=self.show_about)
    
    def create_toolbar(self):
        """إنشاء شريط الأدوات"""
        toolbar = tk.Frame(self.root, bg=self.colors["panel"], height=50)
        toolbar.pack(fill=tk.X, padx=2, pady=2)
        
        # أزرار الأوضاع
        mode_frame = tk.Frame(toolbar, bg=self.colors["panel"])
        mode_frame.pack(side=tk.RIGHT, padx=5)
        
        tk.Label(mode_frame, text="الوضع:", bg=self.colors["panel"], fg=self.colors["text_dim"]).pack(side=tk.RIGHT)
        
        self.mode_var = tk.StringVar(value="object")
        modes = [("كائن", "object"), ("تحرير", "edit"), ("نحت", "sculpt")]
        for text, mode in modes:
            btn = tk.Radiobutton(mode_frame, text=text, variable=self.mode_var, value=mode,
                               bg=self.colors["panel"], fg=self.colors["text"],
                               selectcolor=self.colors["accent"],
                               command=lambda m=mode: self.set_mode(m))
            btn.pack(side=tk.RIGHT, padx=2)
        
        # أزرار الأدوات
        tools_frame = tk.Frame(toolbar, bg=self.colors["panel"])
        tools_frame.pack(side=tk.LEFT, padx=5)
        
        tools = [
            ("🔍", "select", "اختيار (W)"),
            ("✥", "move", "تحريك (G)"),
            ("↻", "rotate", "تدوير (R)"),
            ("⤡", "scale", "تحجيم (S)"),
        ]
        
        self.tool_buttons = {}
        for icon, tool, tip in tools:
            btn = tk.Button(tools_frame, text=icon, width=3, height=1,
                          bg=self.colors["panel"], fg=self.colors["text"],
                          activebackground=self.colors["accent"],
                          command=lambda t=tool: self.set_tool(t))
            btn.pack(side=tk.LEFT, padx=2)
            self.tool_buttons[tool] = btn
            self.create_tooltip(btn, tip)
        
        # فاصل
        sep = tk.Frame(tools_frame, width=2, bg=self.colors["border"])
        sep.pack(side=tk.LEFT, padx=5, fill=tk.Y)
        
        # أزرار الإنشاء
        create_buttons = [
            ("⬜", "cube", "مكعب"),
            ("⭕", "sphere", "كرة"),
            ("🔵", "cylinder", "أسطوانة"),
            ("🔺", "cone", "مخروط"),
            ("▬", "plane", "مستوي"),
            ("💍", "torus", "حلقة"),
        ]
        
        for icon, shape, tip in create_buttons:
            btn = tk.Button(tools_frame, text=icon, width=3, height=1,
                          bg=self.colors["panel"], fg=self.colors["text"],
                          activebackground=self.colors["hover"],
                          command=lambda s=shape: self.add_shape(s))
            btn.pack(side=tk.LEFT, padx=2)
            self.create_tooltip(btn, tip)
    
    def create_main_area(self):
        """إنشاء المنطقة الرئيسية"""
        main_frame = tk.Frame(self.root, bg=self.colors["bg"])
        main_frame.pack(fill=tk.BOTH, expand=True, padx=2, pady=2)
        
        # قائمة الكائنات (يسار)
        self.objects_panel = tk.Frame(main_frame, bg=self.colors["panel"], width=200)
        self.objects_panel.pack(side=tk.LEFT, fill=tk.Y, padx=(0, 2))
        self.objects_panel.pack_propagate(False)
        
        tk.Label(self.objects_panel, text="الكائنات", bg=self.colors["panel"], 
                fg=self.colors["text"], font=("Arial", 12, "bold")).pack(pady=5)
        
        self.objects_list = tk.Frame(self.objects_panel, bg=self.colors["panel"])
        self.objects_list.pack(fill=tk.BOTH, expand=True, padx=5)
        
        # منطقة الرسم (وسط)
        canvas_frame = tk.Frame(main_frame, bg=self.colors["bg"])
        canvas_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        
        self.canvas = tk.Canvas(canvas_frame, bg=self.colors["bg"], highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True)
        
        # لوحة الخصائص (يمين)
        self.props_panel = tk.Frame(main_frame, bg=self.colors["panel"], width=280)
        self.props_panel.pack(side=tk.RIGHT, fill=tk.Y, padx=(2, 0))
        self.props_panel.pack_propagate(False)
        
        self.create_properties_content()
    
    def create_properties_content(self):
        """إنشاء محتوى لوحة الخصائص"""
        # عنوان
        tk.Label(self.props_panel, text="الخصائص", bg=self.colors["panel"], 
                fg=self.colors["text"], font=("Arial", 12, "bold")).pack(pady=5)
        
        # خصائص التحويل
        transform_frame = tk.LabelFrame(self.props_panel, text="التحويل", 
                                       bg=self.colors["panel"], fg=self.colors["text"])
        transform_frame.pack(fill=tk.X, padx=5, pady=5)
        
        # الموضع
        pos_frame = tk.Frame(transform_frame, bg=self.colors["panel"])
        pos_frame.pack(fill=tk.X, padx=5, pady=2)
        
        tk.Label(pos_frame, text="الموضع:", bg=self.colors["panel"], fg=self.colors["text"]).pack(anchor=tk.E)
        
        pos_inputs = tk.Frame(pos_frame, bg=self.colors["panel"])
        pos_inputs.pack(fill=tk.X)
        
        self.pos_vars = {}
        for axis, color in [("X", "#ff4444"), ("Y", "#44ff44"), ("Z", "#4444ff")]:
            frame = tk.Frame(pos_inputs, bg=self.colors["panel"])
            frame.pack(side=tk.LEFT, expand=True, fill=tk.X)
            tk.Label(frame, text=axis, bg=self.colors["panel"], fg=color, width=2).pack(side=tk.LEFT)
            var = tk.DoubleVar(value=0)
            entry = tk.Entry(frame, textvariable=var, width=8, bg=self.colors["bg"], fg=self.colors["text"])
            entry.pack(side=tk.LEFT, padx=2)
            self.pos_vars[axis] = var
        
        # التدوير
        rot_frame = tk.Frame(transform_frame, bg=self.colors["panel"])
        rot_frame.pack(fill=tk.X, padx=5, pady=2)
        
        tk.Label(rot_frame, text="التدوير:", bg=self.colors["panel"], fg=self.colors["text"]).pack(anchor=tk.E)
        
        rot_inputs = tk.Frame(rot_frame, bg=self.colors["panel"])
        rot_inputs.pack(fill=tk.X)
        
        self.rot_vars = {}
        for axis, color in [("X", "#ff4444"), ("Y", "#44ff44"), ("Z", "#4444ff")]:
            frame = tk.Frame(rot_inputs, bg=self.colors["panel"])
            frame.pack(side=tk.LEFT, expand=True, fill=tk.X)
            tk.Label(frame, text=axis, bg=self.colors["panel"], fg=color, width=2).pack(side=tk.LEFT)
            var = tk.DoubleVar(value=0)
            entry = tk.Entry(frame, textvariable=var, width=8, bg=self.colors["bg"], fg=self.colors["text"])
            entry.pack(side=tk.LEFT, padx=2)
            self.rot_vars[axis] = var
        
        # الحجم
        scale_frame = tk.Frame(transform_frame, bg=self.colors["panel"])
        scale_frame.pack(fill=tk.X, padx=5, pady=2)
        
        tk.Label(scale_frame, text="الحجم:", bg=self.colors["panel"], fg=self.colors["text"]).pack(anchor=tk.E)
        
        scale_inputs = tk.Frame(scale_frame, bg=self.colors["panel"])
        scale_inputs.pack(fill=tk.X)
        
        self.scale_vars = {}
        for axis, color in [("X", "#ff4444"), ("Y", "#44ff44"), ("Z", "#4444ff")]:
            frame = tk.Frame(scale_inputs, bg=self.colors["panel"])
            frame.pack(side=tk.LEFT, expand=True, fill=tk.X)
            tk.Label(frame, text=axis, bg=self.colors["panel"], fg=color, width=2).pack(side=tk.LEFT)
            var = tk.DoubleVar(value=1)
            entry = tk.Entry(frame, textvariable=var, width=8, bg=self.colors["bg"], fg=self.colors["text"])
            entry.pack(side=tk.LEFT, padx=2)
            self.scale_vars[axis] = var
        
        # زر تطبيق التحويلات
        tk.Button(transform_frame, text="تطبيق", bg=self.colors["accent"], fg=self.colors["text"],
                 command=self.apply_transform).pack(pady=5)
        
        # خصائص المادة
        material_frame = tk.LabelFrame(self.props_panel, text="المادة", 
                                      bg=self.colors["panel"], fg=self.colors["text"])
        material_frame.pack(fill=tk.X, padx=5, pady=5)
        
        # اللون
        color_frame = tk.Frame(material_frame, bg=self.colors["panel"])
        color_frame.pack(fill=tk.X, padx=5, pady=2)
        
        tk.Label(color_frame, text="اللون:", bg=self.colors["panel"], fg=self.colors["text"]).pack(side=tk.RIGHT)
        
        self.color_preview = tk.Frame(color_frame, bg="#4a90d9", width=30, height=20, relief=tk.RAISED, bd=1)
        self.color_preview.pack(side=tk.RIGHT, padx=5)
        
        tk.Button(color_frame, text="تغيير", bg=self.colors["hover"], fg=self.colors["text"],
                 command=self.change_color).pack(side=tk.RIGHT)
        
        # الشفافية
        opacity_frame = tk.Frame(material_frame, bg=self.colors["panel"])
        opacity_frame.pack(fill=tk.X, padx=5, pady=2)
        
        tk.Label(opacity_frame, text="الشفافية:", bg=self.colors["panel"], fg=self.colors["text"]).pack(anchor=tk.E)
        
        self.opacity_var = tk.IntVar(value=100)
        tk.Scale(opacity_frame, from_=0, to=100, orient=tk.HORIZONTAL, 
                variable=self.opacity_var, bg=self.colors["panel"], fg=self.colors["text"],
                highlightthickness=0).pack(fill=tk.X)
        
        # خصائص الإضاءة
        light_frame = tk.LabelFrame(self.props_panel, text="الإضاءة", 
                                   bg=self.colors["panel"], fg=self.colors["text"])
        light_frame.pack(fill=tk.X, padx=5, pady=5)
        
        # شدة الإضاءة
        intensity_frame = tk.Frame(light_frame, bg=self.colors["panel"])
        intensity_frame.pack(fill=tk.X, padx=5, pady=2)
        
        tk.Label(intensity_frame, text="الشدة:", bg=self.colors["panel"], fg=self.colors["text"]).pack(anchor=tk.E)
        
        self.light_intensity = tk.IntVar(value=100)
        tk.Scale(intensity_frame, from_=0, to=200, orient=tk.HORIZONTAL, 
                variable=self.light_intensity, bg=self.colors["panel"], fg=self.colors["text"],
                highlightthickness=0).pack(fill=tk.X)
    
    def create_status_bar(self):
        """إنشاء شريط الحالة"""
        status_bar = tk.Frame(self.root, bg=self.colors["panel"], height=25)
        status_bar.pack(fill=tk.X, side=tk.BOTTOM)
        
        self.status_left = tk.Label(status_bar, text="وضع الكائن | أداة: اختيار", 
                                   bg=self.colors["panel"], fg=self.colors["text_dim"])
        self.status_left.pack(side=tk.RIGHT, padx=10)
        
        self.status_right = tk.Label(status_bar, text="Easy3D - برنامج التصميم ثلاثي الأبعاد للمبتدئين", 
                                    bg=self.colors["panel"], fg=self.colors["text_dim"])
        self.status_right.pack(side=tk.LEFT, padx=10)
        
        self.fps_label = tk.Label(status_bar, text="FPS: 60", 
                                 bg=self.colors["panel"], fg=self.colors["text_dim"])
        self.fps_label.pack(side=tk.LEFT, padx=10)
    
    def create_tooltip(self, widget, text):
        """إنشاء تلميح"""
        def show(event):
            self.tooltip = tk.Toplevel()
            self.tooltip.wm_overrideredirect(True)
            self.tooltip.wm_geometry(f"+{event.x_root+10}+{event.y_root+10}")
            label = tk.Label(self.tooltip, text=text, bg="#ffffdd", fg="#000000", 
                           relief=tk.SOLID, borderwidth=1)
            label.pack()
        
        def hide(event):
            if hasattr(self, 'tooltip'):
                self.tooltip.destroy()
        
        widget.bind("<Enter>", show)
        widget.bind("<Leave>", hide)
    
    def bind_events(self):
        """ربط الأحداث"""
        # أحداث الماوس
        self.canvas.bind("<Button-1>", self.on_mouse_down)
        self.canvas.bind("<B1-Motion>", self.on_mouse_drag)
        self.canvas.bind("<ButtonRelease-1>", self.on_mouse_up)
        self.canvas.bind("<MouseWheel>", self.on_mouse_wheel)
        self.canvas.bind("<Button-2>", self.on_middle_down)
        self.canvas.bind("<B2-Motion>", self.on_middle_drag)
        self.canvas.bind("<Button-3>", self.on_right_click)
        
        # أحداث لوحة المفاتيح
        self.root.bind("<Key>", self.on_key)
        self.root.bind("<Control-z>", lambda e: self.undo())
        self.root.bind("<Control-y>", lambda e: self.redo())
        self.root.bind("<Control-s>", lambda e: self.save_project())
        self.root.bind("<Control-n>", lambda e: self.new_project())
        self.root.bind("<Control-o>", lambda e: self.load_project())
        self.root.bind("<Delete>", lambda e: self.delete_object())
        self.root.bind("<Shift-D>", lambda e: self.duplicate_object())
        
        # تغيير حجم النافذة
        self.canvas.bind("<Configure>", self.on_resize)
    
    def on_resize(self, event):
        """تغيير حجم منطقة الرسم"""
        self.renderer.width = event.width
        self.renderer.height = event.height
        self.update_viewport()
    
    def on_mouse_down(self, event):
        """النقر بالماوس"""
        self.dragging = True
        self.drag_start = (event.x, event.y)
        
        # محاولة اختيار كائن
        if self.current_tool == "select":
            self.try_select_object(event.x, event.y)
    
    def on_mouse_drag(self, event):
        """سحب الماوس"""
        if not self.drag_start:
            return
        
        dx = event.x - self.drag_start[0]
        dy = event.y - self.drag_start[1]
        
        if self.selected_object and self.current_tool == "move":
            # تحريك الكائن
            speed = 0.01
            self.selected_object.position.x += dx * speed
            self.selected_object.position.y -= dy * speed
            self.update_properties()
            self.drag_start = (event.x, event.y)
        
        elif self.selected_object and self.current_tool == "rotate":
            # تدوير الكائن
            speed = 0.02
            self.selected_object.rotation.y += dx * speed
            self.selected_object.rotation.x += dy * speed
            self.update_properties()
            self.drag_start = (event.x, event.y)
        
        elif self.selected_object and self.current_tool == "scale":
            # تحجيم الكائن
            speed = 0.01
            factor = 1 + dx * speed
            self.selected_object.scale.x *= factor
            self.selected_object.scale.y *= factor
            self.selected_object.scale.z *= factor
            self.update_properties()
            self.drag_start = (event.x, event.y)
        
        self.update_viewport()
    
    def on_mouse_up(self, event):
        """تحرير الماوس"""
        self.dragging = False
        self.drag_start = None
    
    def on_mouse_wheel(self, event):
        """عجلة الماوس - تقريب/تبعيد"""
        zoom_speed = 0.1
        if event.delta > 0:
            self.renderer.camera_pos = self.renderer.camera_pos * (1 - zoom_speed)
        else:
            self.renderer.camera_pos = self.renderer.camera_pos * (1 + zoom_speed)
        self.update_viewport()
    
    def on_middle_down(self, event):
        """زر الماوس الأوسط"""
        self.orbiting = True
        self.orbit_start = (event.x, event.y)
    
    def on_middle_drag(self, event):
        """دوران الكاميرا"""
        if not self.orbit_start:
            return
        
        dx = event.x - self.orbit_start[0]
        dy = event.y - self.orbit_start[1]
        
        # دوران الكاميرا حول الهدف
        angle_x = dx * 0.005
        angle_y = dy * 0.005
        
        # تطبيق الدوران
        pos = self.renderer.camera_pos - self.renderer.camera_target
        pos = rotate_point(pos, angle_y, angle_x, 0)
        self.renderer.camera_target = Vector3(0, 0, 0)
        self.renderer.camera_pos = pos + self.renderer.camera_target
        
        self.orbit_start = (event.x, event.y)
        self.update_viewport()
    
    def on_right_click(self, event):
        """النقر بالزر الأيمن"""
        # قائمة سياق
        menu = tk.Menu(self.root, tearoff=0)
        menu.add_command(label="تكرار", command=self.duplicate_object)
        menu.add_command(label="حذف", command=self.delete_object)
        menu.add_separator()
        menu.add_command(label="إضافة مكعب", command=self.add_cube)
        menu.add_command(label="إضافة كرة", command=self.add_sphere)
        menu.add_separator()
        menu.add_command(label="تغيير اللون", command=self.change_color)
        menu.tk_popup(event.x_root, event.y_root)
    
    def on_key(self, event):
        """أحداث لوحة المفاتيح"""
        key = event.char.lower()
        
        if key == 'w':
            self.set_tool("select")
        elif key == 'g':
            self.set_tool("move")
        elif key == 'r':
            self.set_tool("rotate")
        elif key == 's':
            self.set_tool("scale")
        elif key == 'x':
            self.delete_object()
    
    def try_select_object(self, x, y):
        """محاولة اختيار كائن بالنقر"""
        # بسيط - اختيار بناءً على القرب من المركز
        min_dist = float('inf')
        closest = None
        
        for obj in self.objects:
            p = self.renderer.project_point(obj.position)
            if p:
                dist = math.sqrt((x - p[0])**2 + (y - p[1])**2)
                if dist < min_dist and dist < 50:
                    min_dist = dist
                    closest = obj
        
        # إلغاء تحديد الكل
        for obj in self.objects:
            obj.selected = False
        
        if closest:
            closest.selected = True
            self.selected_object = closest
            self.update_properties()
            self.update_objects_list()
        else:
            self.selected_object = None
            self.clear_properties()
    
    def set_tool(self, tool):
        """تعيين الأداة الحالية"""
        self.current_tool = tool
        for name, btn in self.tool_buttons.items():
            if name == tool:
                btn.configure(bg=self.colors["accent"])
            else:
                btn.configure(bg=self.colors["panel"])
        self.update_status()
    
    def set_mode(self, mode):
        """تعيين الوضع الحالي"""
        self.mode = mode
        self.update_status()
    
    def add_shape(self, shape):
        """إضافة شكل"""
        if shape == "cube":
            self.add_cube()
        elif shape == "sphere":
            self.add_sphere()
        elif shape == "cylinder":
            self.add_cylinder()
        elif shape == "cone":
            self.add_cone()
        elif shape == "plane":
            self.add_plane()
        elif shape == "torus":
            self.add_torus()
    
    def add_cube(self):
        """إضافة مكعب"""
        mesh = create_cube()
        self.add_object(mesh)
    
    def add_sphere(self):
        """إضافة كرة"""
        mesh = create_sphere()
        self.add_object(mesh)
    
    def add_cylinder(self):
        """إضافة أسطوانة"""
        mesh = create_cylinder()
        self.add_object(mesh)
    
    def add_cone(self):
        """إضافة مخروط"""
        mesh = create_cone()
        self.add_object(mesh)
    
    def add_plane(self):
        """إضافة مستوي"""
        mesh = create_plane()
        self.add_object(mesh)
    
    def add_torus(self):
        """إضافة حلقة"""
        mesh = create_torus()
        self.add_object(mesh)
    
    def add_object(self, mesh):
        """إضافة كائن للمشهد"""
        self.objects.append(mesh)
        self.select_object(mesh)
        self.update_objects_list()
        self.update_viewport()
        self.save_state()
    
    def select_object(self, obj):
        """تحديد كائن"""
        for o in self.objects:
            o.selected = False
        obj.selected = True
        self.selected_object = obj
        self.update_properties()
        self.update_objects_list()
    
    def delete_object(self):
        """حذف الكائن المحدد"""
        if self.selected_object:
            self.objects.remove(self.selected_object)
            self.selected_object = None
            self.clear_properties()
            self.update_objects_list()
            self.update_viewport()
            self.save_state()
    
    def duplicate_object(self):
        """تكرار الكائن المحدد"""
        if self.selected_object:
            new_mesh = Mesh3D(
                self.selected_object.name + " (نسخة)",
                [v.copy() for v in self.selected_object.vertices],
                [f[:] for f in self.selected_object.faces]
            )
            new_mesh.position = self.selected_object.position + Vector3(1, 0, 0)
            new_mesh.rotation = self.selected_object.rotation.copy()
            new_mesh.scale = self.selected_object.scale.copy()
            new_mesh.material = Material(
                self.selected_object.material.color,
                self.selected_object.material.metalness,
                self.selected_object.material.roughness,
                self.selected_object.material.opacity
            )
            self.add_object(new_mesh)
    
    def update_properties(self):
        """تحديث لوحة الخصائص"""
        if not self.selected_object:
            return
        
        obj = self.selected_object
        
        self.pos_vars["X"].set(round(obj.position.x, 2))
        self.pos_vars["Y"].set(round(obj.position.y, 2))
        self.pos_vars["Z"].set(round(obj.position.z, 2))
        
        self.rot_vars["X"].set(round(math.degrees(obj.rotation.x), 1))
        self.rot_vars["Y"].set(round(math.degrees(obj.rotation.y), 1))
        self.rot_vars["Z"].set(round(math.degrees(obj.rotation.z), 1))
        
        self.scale_vars["X"].set(round(obj.scale.x, 2))
        self.scale_vars["Y"].set(round(obj.scale.y, 2))
        self.scale_vars["Z"].set(round(obj.scale.z, 2))
        
        self.color_preview.configure(bg=obj.material.color)
    
    def clear_properties(self):
        """مسح الخصائص"""
        for var in self.pos_vars.values():
            var.set(0)
        for var in self.rot_vars.values():
            var.set(0)
        for var in self.scale_vars.values():
            var.set(1)
    
    def apply_transform(self):
        """تطبيق التحويلات"""
        if not self.selected_object:
            return
        
        obj = self.selected_object
        
        obj.position.x = self.pos_vars["X"].get()
        obj.position.y = self.pos_vars["Y"].get()
        obj.position.z = self.pos_vars["Z"].get()
        
        obj.rotation.x = math.radians(self.rot_vars["X"].get())
        obj.rotation.y = math.radians(self.rot_vars["Y"].get())
        obj.rotation.z = math.radians(self.rot_vars["Z"].get())
        
        obj.scale.x = self.scale_vars["X"].get()
        obj.scale.y = self.scale_vars["Y"].get()
        obj.scale.z = self.scale_vars["Z"].get()
        
        self.update_viewport()
        self.save_state()
    
    def change_color(self):
        """تغيير لون الكائن"""
        if not self.selected_object:
            return
        
        color = colorchooser.askcolor(self.selected_object.material.color)[1]
        if color:
            self.selected_object.material.color = color
            self.color_preview.configure(bg=color)
            self.update_viewport()
            self.save_state()
    
    def update_objects_list(self):
        """تحديث قائمة الكائنات"""
        for widget in self.objects_list.winfo_children():
            widget.destroy()
        
        for i, obj in enumerate(self.objects):
            frame = tk.Frame(self.objects_list, bg=self.colors["panel"])
            frame.pack(fill=tk.X, pady=1)
            
            bg = self.colors["selected"] if obj.selected else self.colors["panel"]
            
            btn = tk.Button(frame, text=obj.name, anchor=tk.E,
                          bg=bg, fg=self.colors["text"],
                          command=lambda o=obj: self.select_object(o))
            btn.pack(fill=tk.X)
            
            # زر الحذف
            del_btn = tk.Button(frame, text="✕", width=2,
                              bg=self.colors["panel"], fg="#ff4444",
                              command=lambda o=obj: self.delete_specific(o))
            del_btn.pack(side=tk.LEFT)
    
    def delete_specific(self, obj):
        """حذف كائن محدد"""
        if obj in self.objects:
            self.objects.remove(obj)
            if self.selected_object == obj:
                self.selected_object = None
                self.clear_properties()
            self.update_objects_list()
            self.update_viewport()
            self.save_state()
    
    def update_status(self):
        """تحديث شريط الحالة"""
        mode_text = {"object": "وضع الكائن", "edit": "وضع التحرير", "sculpt": "وضع النحت"}
        tool_text = {"select": "اختيار", "move": "تحريك", "rotate": "تدوير", "scale": "تحجيم"}
        
        self.status_left.configure(text=f"{mode_text.get(self.mode, '')} | أداة: {tool_text.get(self.current_tool, '')}")
    
    def update_viewport(self):
        """تحديث منطقة الرسم"""
        self.renderer.render(self.objects)
        self.root.after(33, self.update_viewport)  # ~30 FPS
    
    def set_view(self, view):
        """تعيين زاوية الكاميرا"""
        self.renderer.set_camera_preset(view)
        self.update_viewport()
    
    def reset_camera(self):
        """إعادة تعيين الكاميرا"""
        self.renderer.camera_pos = Vector3(3, 3, 3)
        self.renderer.camera_target = Vector3(0, 0, 0)
        self.update_viewport()
    
    def save_state(self):
        """حفظ الحالة للتراجع/الإعادة"""
        state = [obj.to_dict() for obj in self.objects]
        self.history = self.history[:self.history_index + 1]
        self.history.append(state)
        self.history_index = len(self.history) - 1
    
    def undo(self):
        """تراجع"""
        if self.history_index > 0:
            self.history_index -= 1
            self.load_state(self.history[self.history_index])
    
    def redo(self):
        """إعادة"""
        if self.history_index < len(self.history) - 1:
            self.history_index += 1
            self.load_state(self.history[self.history_index])
    
    def load_state(self, state):
        """تحميل حالة"""
        self.objects = [Mesh3D.from_dict(d) for d in state]
        self.selected_object = None
        self.update_objects_list()
        self.update_viewport()
    
    def new_project(self):
        """مشروع جديد"""
        if messagebox.askyesno("جديد", "هل تريد إنشاء مشروع جديد؟ سيتم فقدان العمل الحالي."):
            self.objects = []
            self.selected_object = None
            self.history = []
            self.history_index = -1
            self.clear_properties()
            self.update_objects_list()
            self.update_viewport()
    
    def save_project(self):
        """حفظ المشروع"""
        filepath = filedialog.asksaveasfilename(
            defaultextension=".e3d",
            filetypes=[("Easy3D Files", "*.e3d"), ("All Files", "*.*")]
        )
        if filepath:
            data = {
                "version": "1.0",
                "objects": [obj.to_dict() for obj in self.objects],
                "camera": {
                    "position": self.renderer.camera_pos.to_tuple(),
                    "target": self.renderer.camera_target.to_tuple()
                }
            }
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            self.status_right.configure(text=f"تم الحفظ: {filepath}")
    
    def load_project(self):
        """فتح مشروع"""
        filepath = filedialog.askopenfilename(
            filetypes=[("Easy3D Files", "*.e3d"), ("All Files", "*.*")]
        )
        if filepath:
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                self.objects = [Mesh3D.from_dict(d) for d in data["objects"]]
                self.selected_object = None
                self.update_objects_list()
                self.update_viewport()
                self.status_right.configure(text=f"تم الفتح: {filepath}")
            except Exception as e:
                messagebox.showerror("خطأ", f"فشل فتح الملف: {e}")
    
    def export_obj(self):
        """تصدير OBJ"""
        filepath = filedialog.asksaveasfilename(
            defaultextension=".obj",
            filetypes=[("OBJ Files", "*.obj"), ("All Files", "*.*")]
        )
        if filepath:
            try:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write("# Easy3D Export\n")
                    f.write(f"# Objects: {len(self.objects)}\n\n")
                    
                    vertex_offset = 0
                    for obj in self.objects:
                        f.write(f"# {obj.name}\n")
                        
                        world_verts = obj.get_world_vertices()
                        for v in world_verts:
                            f.write(f"v {v.x} {v.y} {v.z}\n")
                        
                        for face in obj.faces:
                            face_str = " ".join(str(i + vertex_offset + 1) for i in face)
                            f.write(f"f {face_str}\n")
                        
                        vertex_offset += len(world_verts)
                        f.write("\n")
                
                self.status_right.configure(text=f"تم التصدير: {filepath}")
            except Exception as e:
                messagebox.showerror("خطأ", f"فشل التصدير: {e}")
    
    def show_shortcuts(self):
        """عرض الاختصارات"""
        shortcuts = """
الاختصارات:

الأدوات:
W - اختيار
G - تحريك
R - تدوير
S - تحجيم

الكاميرا:
عجلة الماوس - تقريب/تبعيد
الزر الأوسط - دوران
Shift + زر وسط - تحريك

أخرى:
Shift+D - تكرار
Delete/X - حذف
Ctrl+Z - تراجع
Ctrl+Y - إعادة
Ctrl+S - حفظ
Ctrl+N - جديد
Ctrl+O - فتح

أزرار Numpad:
1 - أمامي
3 - يمين
7 - علوي
+ Ctrl للاتجاه المعاكس
        """
        messagebox.showinfo("الاختصارات", shortcuts)
    
    def show_about(self):
        """حول البرنامج"""
        about = """
Easy3D - برنامج التصميم ثلاثي الأبعاد

الإصدار 1.0

برنامج بسيط وخفيف لتصميم الأشكال ثلاثية الأبعاد.
يعمل بدون كارت شاشة و مناسب للأجهزة الضعيفة.

المميزات:
- إنشاء الأشكال الأساسية
- تحريك و تدوير و تحجيم
- تغيير الألوان و المواد
- حفظ و تحميل المشاريع
- تصدير OBJ

تم التطوير باستخدام Python و Tkinter
        """
        messagebox.showinfo("حول البرنامج", about)
    
    def run(self):
        """تشغيل التطبيق"""
        self.root.mainloop()


# ==================== تشغيل البرنامج ====================

if __name__ == "__main__":
    app = Easy3DApp()
    app.run()
