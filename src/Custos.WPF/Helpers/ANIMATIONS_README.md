# Руководство по анимациям в Custos

## Обзор улучшений

Приложение теперь использует улучшенную систему анимаций для более плавного и приятного UX:

### ✨ Что было улучшено:

1. **Плавные переходы между view** - Slide + Fade вместо простого fade
2. **Hardware acceleration** - Лучшая производительность анимаций
3. **Улучшенные easing functions** - Более естественные кривые движения (BackEase, CubicEase)
4. **Smooth scrolling** - Плавная прокрутка для всех ScrollViewer
5. **Анимации overlay** - Scale + Fade для popup окон
6. **Micro-interactions** - Улучшенные hover эффекты для кнопок

---

## AnimationHelper - Готовые анимации

### Основные методы:

#### 1. FadeIn / FadeOut
```csharp
// Плавное появление
AnimationHelper.FadeIn(myElement);

// Плавное исчезновение с коллапсом
AnimationHelper.FadeOut(myElement, collapseOnComplete: true);
```

#### 2. SlideAndFadeIn / SlideAndFadeOut
```csharp
// Слайд снизу + fade (идеально для смены view)
AnimationHelper.SlideAndFadeIn(myElement, slideDistance: 30);

// Слайд вверх + fade при исчезновении
AnimationHelper.SlideAndFadeOut(myElement, slideDistance: -30);
```

#### 3. ScaleAndFadeIn / ScaleAndFadeOut
```csharp
// Масштабирование + fade (идеально для overlay/popup)
AnimationHelper.ScaleAndFadeIn(myElement, fromScale: 0.9);

// Обратная анимация
AnimationHelper.ScaleAndFadeOut(myElement);
```

#### 4. Stagger анимации
```csharp
// Последовательное появление элементов с задержкой
AnimationHelper.StaggerIn(myStackPanel.Children, delayMs: 50);
```

#### 5. Специальные эффекты
```csharp
// Shake - для ошибок или привлечения внимания
AnimationHelper.Shake(myElement, intensity: 10);

// Pulse - легкое пульсирование
AnimationHelper.Pulse(myElement, toScale: 1.05);
```

### Стандартные настройки:

- **FastDuration** = 200ms
- **NormalDuration** = 300ms
- **SlowDuration** = 400ms

- **EaseOutCubic** - плавное замедление
- **EaseOutBack** - пружинящий эффект (amplitude: 0.5)
- **EaseOutQuart** - более сильное замедление

---

## SmoothScrollBehavior - Плавная прокрутка

### Использование в XAML:

```xml
<ScrollViewer behaviors:SmoothScrollBehavior.IsEnabled="True"
              VerticalScrollBarVisibility="Auto">
    <!-- Контент -->
</ScrollViewer>
```

### Параметры:
- **ScrollSpeed** = 1.2 (множитель скорости)
- **AnimationDuration** = 400ms
- **EasingFunction** = CubicEase.EaseOut

---

## Улучшенные стили кнопок

### PrimaryButtonStyle
- Hover: Scale 1.03 с BackEase (пружинящий эффект)
- Press: Scale 0.97 с QuadraticEase
- Все переходы цвета плавные (200-300ms)

### OutlinedButtonStyle
- Hover: Scale 1.02 + background fade
- Плавные border transitions

### SidebarButtonStyle
- Hover: Scale 1.1 с индикатором слева
- Press: Scale 0.95

---

## Hardware Acceleration

### Включение:
```csharp
AnimationHelper.EnableHardwareAcceleration(myElement);
```

### Отключение после анимации:
```csharp
AnimationHelper.DisableHardwareAcceleration(myElement);
```

**Примечание:** Hardware acceleration автоматически включается/отключается в методах анимации.

---

## Примеры использования

### Пример 1: Анимация при смене view
```csharp
private void AnimateContentChange()
{
    AnimationHelper.SlideAndFadeIn(
        MainContentControl,
        slideDistance: 30,
        duration: AnimationHelper.NormalDuration,
        easing: AnimationHelper.EaseOutCubic
    );
}
```

### Пример 2: Анимация popup overlay
```xaml
<Border RenderTransformOrigin="0.5,0.5">
    <Border.Style>
        <Style TargetType="Border">
            <Style.Triggers>
                <Trigger Property="Visibility" Value="Visible">
                    <Trigger.EnterActions>
                        <BeginStoryboard>
                            <Storyboard>
                                <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                                 From="0.9" To="1" Duration="0:0:0.35">
                                    <DoubleAnimation.EasingFunction>
                                        <BackEase EasingMode="EaseOut" Amplitude="0.5"/>
                                    </DoubleAnimation.EasingFunction>
                                </DoubleAnimation>
                            </Storyboard>
                        </BeginStoryboard>
                    </Trigger.EnterActions>
                </Trigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
</Border>
```

### Пример 3: Плавная смена цвета
```csharp
AnimationHelper.AnimateColor(
    myBrush,
    SolidColorBrush.ColorProperty,
    Colors.Orange
);
```

---

## Рекомендации по производительности

1. **Используйте Hardware Acceleration** для сложных анимаций
2. **Не злоупотребляйте одновременными анимациями** - максимум 3-4 элемента одновременно
3. **Отключайте Hardware Acceleration** после завершения анимации
4. **Используйте CacheMode** только во время анимации
5. **Предпочитайте Transform анимации** вместо Layout изменений

---

## Troubleshooting

### Анимация тормозит
- Убедитесь, что Hardware Acceleration включен
- Проверьте, что не анимируете слишком много элементов одновременно
- Используйте более быстрые Duration (FastDuration)

### Элементы "прыгают"
- Убедитесь, что RenderTransformOrigin установлен правильно (обычно 0.5,0.5)
- Проверьте, что Layout не перерасчитывается во время анимации

### Анимации не плавные
- Используйте правильные EasingFunction (CubicEase, BackEase)
- Увеличьте Duration для более заметного эффекта
- Проверьте производительность системы

---

## Дальнейшие улучшения

### Возможные будущие добавления:
- [ ] Particle effects для специальных событий
- [ ] Liquid/fluid анимации для индикаторов прогресса
- [ ] Parallax эффекты для фоновых элементов
- [ ] Morphing transitions между shapes
- [ ] Physics-based animations с SpringAnimation
