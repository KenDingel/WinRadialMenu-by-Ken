using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RadialMenu.Controls
{
    /// <summary>
    /// A modern fantasy-style particle system that creates a spiral of energy particles
    /// </summary>
    public class EnergyParticleSystem : Canvas
    {
        private readonly List<EnergyParticle> _particles = new();
        private readonly DispatcherTimer _animationTimer;
        private readonly Random _random = new();
        private Point _centerPoint;
        private double _maxRadius = 300;
        private bool _isActive = false;
        private Storyboard? _currentAnimation;
        private DateTime _animationStartTime;
        
        // Configuration properties
        public int ParticleCount { get; set; } = 24;
        public double SpiralTurns { get; set; } = 3.0;
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(1200);
        public Color PrimaryColor { get; set; } = Color.FromArgb(255, 120, 200, 255); // Cyan
        public Color SecondaryColor { get; set; } = Color.FromArgb(255, 180, 100, 255); // Purple
        public double ParticleSize { get; set; } = 6.0;
        public double UIScale { get; set; } = 1.0;
        public bool TestMode { get; set; } = false; // Test mode for debugging

        public EnergyParticleSystem()
        {
            Background = Brushes.Transparent;
            IsHitTestVisible = false; // Don't interfere with menu interactions
            
            // Timer for particle movement - 60 FPS for buttery smooth animation
            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS for smooth motion
            };
            _animationTimer.Tick += UpdateParticles;
        }
        
        private void CreateTestRectangle()
        {
            // Create a bright rectangle that should be impossible to miss
            var testRect = new System.Windows.Shapes.Rectangle
            {
                Width = 300,
                Height = 300,
                Fill = new SolidColorBrush(Colors.Magenta),
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 10,
                Opacity = 0.8
            };
            
            // Position it in the center of the canvas
            Canvas.SetLeft(testRect, (Width / 2) - 150);
            Canvas.SetTop(testRect, (Height / 2) - 150);
            Canvas.SetZIndex(testRect, 100); // Very high Z-index
            
            Children.Add(testRect);
            
            // Also add some corner markers to verify canvas bounds
            var corners = new[]
            {
                new { X = 0.0, Y = 0.0, Color = Colors.Red },
                new { X = Width - 50, Y = 0.0, Color = Colors.Green },
                new { X = 0.0, Y = Height - 50, Color = Colors.Blue },
                new { X = Width - 50, Y = Height - 50, Color = Colors.Orange }
            };
            
            foreach (var corner in corners)
            {
                var marker = new Ellipse
                {
                    Width = 50,
                    Height = 50,
                    Fill = new SolidColorBrush(corner.Color),
                    Opacity = 0.9
                };
                Canvas.SetLeft(marker, corner.X);
                Canvas.SetTop(marker, corner.Y);
                Canvas.SetZIndex(marker, 99);
                Children.Add(marker);
            }
        }

        /// <summary>
        /// Start the energy spiral animation from the center point
        /// </summary>
        public void StartEnergySpiral(Point center, double maxRadius)
        {
            if (_isActive) return;
            
            _centerPoint = center;
            _maxRadius = maxRadius * UIScale;
            _isActive = true;
            _animationStartTime = DateTime.UtcNow;
            
            // Debug logging to track particle system activation
            System.Diagnostics.Debug.WriteLine($"[EnergyParticleSystem] Starting particle spiral - Center: {center}, MaxRadius: {maxRadius}, Scaled: {_maxRadius}, ParticleCount: {ParticleCount}, Canvas Size: {Width}x{Height}");
            
            CreateParticles();
            
            if (TestMode)
            {
                CreateStaticTestParticles();
            }
            else
            {
                StartAnimation();
            }
        }

        /// <summary>
        /// Create static visible particles for testing
        /// </summary>
        private void CreateStaticTestParticles()
        {
            // Create large, bright, unmissable circles
            for (int i = 0; i < 8; i++)
            {
                var angle = i * 45; // Every 45 degrees
                var distance = 150;
                
                var angleRadians = angle * Math.PI / 180.0;
                var x = _centerPoint.X + Math.Cos(angleRadians) * distance;
                var y = _centerPoint.Y + Math.Sin(angleRadians) * distance;
                
                var ellipse = new Ellipse
                {
                    Width = 50, // Much larger
                    Height = 50,
                    Fill = new SolidColorBrush(Colors.Cyan), // Bright cyan
                    Stroke = new SolidColorBrush(Colors.Red), // Red border
                    StrokeThickness = 3,
                    Opacity = 1.0
                };
                
                Canvas.SetLeft(ellipse, x - 25);
                Canvas.SetTop(ellipse, y - 25);
                Canvas.SetZIndex(ellipse, 50);
                Children.Add(ellipse);
            }
            
            // Also add a large circle at the center
            var centerCircle = new Ellipse
            {
                Width = 100,
                Height = 100,
                Fill = new SolidColorBrush(Colors.Orange),
                Stroke = new SolidColorBrush(Colors.Purple),
                StrokeThickness = 5,
                Opacity = 1.0
            };
            
            Canvas.SetLeft(centerCircle, _centerPoint.X - 50);
            Canvas.SetTop(centerCircle, _centerPoint.Y - 50);
            Canvas.SetZIndex(centerCircle, 60);
            Children.Add(centerCircle);
        }

        /// <summary>
        /// Stop the particle animation and clear particles
        /// </summary>
        public void Stop()
        {
            _isActive = false;
            _animationTimer.Stop();
            
            // Immediately clear all particles - no fade animation to prevent interference
            Children.Clear();
            _particles.Clear();
            _currentAnimation?.Stop();
            _currentAnimation = null;
            
            System.Diagnostics.Debug.WriteLine($"[EnergyParticleSystem] Stopped - Particles cleared, _isActive = {_isActive}");
        }

        private void CreateParticles()
        {
            Children.Clear();
            _particles.Clear();

            // Create multiple spiral arms for more fantasy effect
            var spiralArms = 3;
            var particlesPerArm = ParticleCount / spiralArms;
            
            System.Diagnostics.Debug.WriteLine($"[EnergyParticleSystem] Creating {ParticleCount} particles in {spiralArms} arms ({particlesPerArm} per arm)");
            
            for (int arm = 0; arm < spiralArms; arm++)
            {
                var armOffset = arm * (360.0 / spiralArms);
                
                for (int i = 0; i < particlesPerArm; i++)
                {
                    // Create organic spiral pattern with varying density
                    var normalizedIndex = (double)i / particlesPerArm;
                    var angle = armOffset + (normalizedIndex * 360.0 * SpiralTurns);
                    
                    // Add some randomness to make it more organic
                    angle += (_random.NextDouble() - 0.5) * 15.0;
                    
                    // Vary starting distance for layered effect
                    var baseDistance = 10 * UIScale;
                    var distance = baseDistance + (_random.NextDouble() * 20 * UIScale);
                    
                    var particleIndex = arm * particlesPerArm + i;
                    var particle = CreateParticle(particleIndex, angle, distance, arm);
                    _particles.Add(particle);
                    Children.Add(particle.Visual);
                    
                    System.Diagnostics.Debug.WriteLine($"[EnergyParticleSystem] Created particle {particleIndex}: Angle={angle:F1}, Distance={distance:F1}, Size={particle.Size:F1}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[EnergyParticleSystem] Total particles created: {_particles.Count}, Children count: {Children.Count}");
        }

        private EnergyParticle CreateParticle(int index, double targetAngle, double startDistance, int armIndex = 0)
        {
            // Multiple size variations for more dynamic look
            var sizeMultipliers = new[] { 0.7, 0.9, 1.1, 1.3, 1.5, 1.8 }; // 6 different sizes
            var sizeMultiplier = sizeMultipliers[_random.Next(sizeMultipliers.Length)];
            var size = ParticleSize * UIScale * sizeMultiplier;
            
            // Create particle visual - no expensive effects, just solid color
            var ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = CreateParticleBrush(index, armIndex),
                // NO EFFECTS - old school performance trick
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            // Position at center initially
            Canvas.SetLeft(ellipse, _centerPoint.X - size / 2);
            Canvas.SetTop(ellipse, _centerPoint.Y - size / 2);

            return new EnergyParticle
            {
                Visual = ellipse,
                TargetAngle = targetAngle,
                StartDistance = startDistance,
                Index = index,
                Size = size,
                ArmIndex = armIndex
            };
        }

        private Brush CreateParticleBrush(int index, int armIndex = 0)
        {
            // Old school trick: Simple solid colors instead of expensive gradients
            var color = armIndex switch
            {
                1 => Color.FromArgb(255, 200, 120, 255), // Golden purple
                2 => Color.FromArgb(255, 150, 255, 180), // Mint green
                _ => Color.FromArgb(255, 100, 220, 255)   // Electric cyan
            };
            
            var brush = new SolidColorBrush(color);
            brush.Freeze(); // Performance: freeze brushes to avoid change notifications
            return brush;
        }

        private Effect CreateGlowEffect(int armIndex = 0, double particleSize = 0)
        {
            var glowColor = armIndex switch
            {
                1 => Color.FromArgb(220, 200, 120, 255), // Purple-gold glow - very opaque
                2 => Color.FromArgb(220, 150, 255, 150), // Emerald glow - very opaque
                _ => Color.FromArgb(220, 100, 220, 255)  // Electric blue glow - very opaque
            };
            
            // Scale blur radius based on particle size for performance
            var baseBlur = Math.Max(8, particleSize * 0.8); // Adaptive blur based on particle size
            var blurRadius = Math.Min(baseBlur * UIScale, 35 * UIScale); // Cap maximum blur
            
            return new DropShadowEffect
            {
                Color = glowColor,
                BlurRadius = blurRadius,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = 0.9 // Slightly reduced for better performance
            };
        }

        private void StartAnimation()
        {
            // Simplified animation - all handled in UpdateParticles at 60 FPS
            _animationTimer.Start();
            
            // Set up auto-stop after animation duration
            var stopTimer = new DispatcherTimer
            {
                Interval = AnimationDuration + TimeSpan.FromMilliseconds(300) // Extra time for fade out
            };
            stopTimer.Tick += (s, e) =>
            {
                stopTimer.Stop();
                _animationTimer.Stop();
                _isActive = false;
            };
            stopTimer.Start();
        }

        private void UpdateParticles(object? sender, EventArgs e)
        {
            if (!_isActive) return;

            // Calculate elapsed time once for all particles (old school optimization)
            var elapsedTime = DateTime.UtcNow - _animationStartTime;
            var baseProgress = Math.Min(elapsedTime.TotalMilliseconds / AnimationDuration.TotalMilliseconds, 1.0);
            
            // Early exit if animation is complete
            if (baseProgress >= 1.0)
            {
                _animationTimer.Stop();
                return;
            }

            // Process particles efficiently - fewer calculations per particle
            for (int i = 0; i < _particles.Count; i++)
            {
                var particle = _particles[i];
                
                // Calculate particle-specific progress with staggered start
                var particleProgress = Math.Max(0, baseProgress - (i * 0.015));
                if (particleProgress <= 0) 
                {
                    particle.Visual.Opacity = 0; // Hide particles that haven't started
                    continue;
                }

                // Simple easing - old school cubic ease out
                var t = particleProgress;
                var easedProgress = 1 - Math.Pow(1 - t, 3);
                
                // Calculate spiral position (optimized trigonometry)
                var angleRadians = (particle.TargetAngle * easedProgress) * 0.017453292519943295; // PI/180 pre-computed
                var distance = particle.StartDistance + (_maxRadius * easedProgress);
                
                // Calculate final position
                var halfSize = particle.Size * 0.5;
                var x = _centerPoint.X + Math.Cos(angleRadians) * distance - halfSize;
                var y = _centerPoint.Y + Math.Sin(angleRadians) * distance - halfSize;
                
                // Update position
                Canvas.SetLeft(particle.Visual, x);
                Canvas.SetTop(particle.Visual, y);
                
                // Fade out while moving - particles fade as they move outward
                var fadeProgress = Math.Min(particleProgress * 1.5, 1.0); // Fade faster than movement
                var opacity = fadeProgress < 0.6 ? 
                    Math.Min(fadeProgress * 2.0, 1.0) : // Fade in quickly
                    Math.Max(1.0 - ((fadeProgress - 0.6) * 2.5), 0.0); // Then fade out gradually
                
                particle.Visual.Opacity = opacity;
            }
        }

        private static double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        internal class EnergyParticle
        {
            public required Ellipse Visual { get; set; }
            public required double TargetAngle { get; set; }
            public required double StartDistance { get; set; }
            public required int Index { get; set; }
            public required double Size { get; set; }
            public int ArmIndex { get; set; }
        }
    }
}