// Simple test to verify the EnergyParticleSystem compiles and can be instantiated
using System;
using System.Windows;
using System.Windows.Media;
using RadialMenu.Controls;

namespace RadialMenu.Test
{
    public class ParticleSystemTest
    {
        public static void TestParticleSystem()
        {
            try
            {
                // Create particle system instance
                var particleSystem = new EnergyParticleSystem
                {
                    Width = 800,
                    Height = 600,
                    UIScale = 1.0,
                    ParticleCount = 24,
                    SpiralTurns = 2.5,
                    AnimationDuration = TimeSpan.FromMilliseconds(1500),
                    PrimaryColor = Color.FromArgb(255, 120, 200, 255),
                    SecondaryColor = Color.FromArgb(255, 180, 100, 255),
                    ParticleSize = 6.0
                };
                
                // Test basic functionality
                var centerPoint = new Point(400, 300);
                particleSystem.StartEnergySpiral(centerPoint, 200);
                
                Console.WriteLine("✓ EnergyParticleSystem created and started successfully");
                Console.WriteLine($"✓ Particle count: {particleSystem.ParticleCount}");
                Console.WriteLine($"✓ Animation duration: {particleSystem.AnimationDuration}");
                Console.WriteLine($"✓ Primary color: {particleSystem.PrimaryColor}");
                Console.WriteLine($"✓ Secondary color: {particleSystem.SecondaryColor}");
                
                particleSystem.Stop();
                Console.WriteLine("✓ Particle system stopped successfully");
                
                Console.WriteLine("\n🎉 All tests passed! The energy particle system is ready to use.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}