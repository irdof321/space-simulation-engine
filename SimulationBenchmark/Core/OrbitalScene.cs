using System;
using System.Collections.Generic;
using System.Drawing;
using PhysicsSimulation;

// Visualization metadata is kept OUTSIDE of the PhysicsSimulation engine.
internal sealed class BodyMarker
{
    public string Name { get; }
    public CelestBody Body { get; }
    public Color Color { get; }
    public float RadiusPixels { get; }
    public bool IsSatellite { get; }
    public bool IsStar { get; }
    public bool IsComet { get; }

    public BodyMarker(string name, CelestBody body, Color color, float radiusPixels, bool isSatellite, bool isStar, bool isComet)
    {
        Name = name;
        Body = body;
        Color = color;
        RadiusPixels = radiusPixels;
        IsSatellite = isSatellite;
        IsStar = isStar;
        IsComet = isComet;
    }
}

internal sealed class OrbitView
{
    public string Title { get; }
    public string CenterBodyName { get; }
    public double HalfRangeMeters { get; }
    public bool ShowSatellites { get; }
    public bool StarsOnly { get; }
    public HashSet<string>? VisibleBodyNames { get; }

    public OrbitView(string title, string centerBodyName, double halfRangeMeters,
        bool showSatellites, bool starsOnly, string[]? visibleBodyNames = null)
    {
        if (!double.IsFinite(halfRangeMeters) || halfRangeMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(halfRangeMeters));

        Title = title;
        CenterBodyName = centerBodyName;
        HalfRangeMeters = halfRangeMeters;
        ShowSatellites = showSatellites;
        StarsOnly = starsOnly;
        VisibleBodyNames = visibleBodyNames == null
            ? null
            : new HashSet<string>(visibleBodyNames, StringComparer.OrdinalIgnoreCase);
    }
}

internal enum DistanceUnit
{
    AstronomicalUnits,
    Kilometers,
    LightYears
}

internal sealed class DistanceReadout
{
    public string Label { get; }
    public string FirstBodyName { get; }
    public string SecondBodyName { get; }
    public DistanceUnit Unit { get; }

    public DistanceReadout(string label, string firstBodyName, string secondBodyName, DistanceUnit unit)
    {
        Label = label;
        FirstBodyName = firstBodyName;
        SecondBodyName = secondBodyName;
        Unit = unit;
    }
}

// The scene owns the orbital hierarchy, named references, views and status readouts.
// This class does not implement gravity, Kepler equations, or numerical integration.
internal sealed class OrbitalScene
{
    private readonly List<BodyMarker> _markers = new();
    private readonly Dictionary<string, BodyMarker> _byName =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<OrbitView> _views = new();
    private readonly List<DistanceReadout> _readouts = new();

    public OrbitalSystem Root { get; } = new OrbitalSystem();
    public IReadOnlyList<BodyMarker> Markers => _markers;
    public IReadOnlyList<OrbitView> Views => _views;
    public IReadOnlyList<DistanceReadout> Readouts => _readouts;

    public CelestBody AddBody(
        OrbitalSystem parent,
        string name,
        double mass,
        Color color,
        OrbitalParameters? orbit = null,
        bool isSatellite = false,
        float radiusPixels = 4.0f,
        bool isStar = false,
        bool isComet = false)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));
        if (string.IsNullOrWhiteSpace(name) || _byName.ContainsKey(name))
            throw new ArgumentException("Body name must be nonempty and unique.", nameof(name));
        if (!double.IsFinite(mass) || mass <= 0)
            throw new ArgumentOutOfRangeException(nameof(mass));
        if (radiusPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusPixels));

        var body = new CelestBody(mass, new Vector3Double(0, 0, 0));
        parent.AddMember(new OrbitalMember(body, orbit));
        var marker = new BodyMarker(name, body, color, radiusPixels, isSatellite, isStar, isComet);
        _markers.Add(marker);
        _byName.Add(name, marker);
        return body;
    }

    public OrbitalSystem AddSystem(OrbitalSystem parent, OrbitalParameters? orbit = null)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));
        // No orbital parameters are required for the primary (first) member.
        var child = new OrbitalSystem();
        parent.AddMember(new OrbitalMember(child, orbit));
        return child;
    }

    public BodyMarker GetMarker(string name)
    {
        if (!_byName.TryGetValue(name, out BodyMarker? marker))
            throw new KeyNotFoundException($"Unknown celestial body: {name}.");
        return marker;
    }

    public void AddView(string title, string centerName, double halfRangeMeters,
        bool showSatellites = false, bool starsOnly = false, string[]? visibleBodyNames = null)
    {
        GetMarker(centerName);
        if (visibleBodyNames != null)
        {
            foreach (string name in visibleBodyNames)
                GetMarker(name);
        }
        _views.Add(new OrbitView(title, centerName, halfRangeMeters,
            showSatellites, starsOnly, visibleBodyNames));
    }

    public void AddDistanceReadout(string label, string firstName, string secondName, DistanceUnit unit)
    {
        GetMarker(firstName);
        GetMarker(secondName);
        _readouts.Add(new DistanceReadout(label, firstName, secondName, unit));
    }

    public string FormatDistance(DistanceReadout readout)
    {
        var first = GetMarker(readout.FirstBodyName).Body;
        var second = GetMarker(readout.SecondBodyName).Body;
        double meters = (first.Position - second.Position).GetMagnitude();
        return readout.Unit switch
        {
            DistanceUnit.AstronomicalUnits => $"{meters / AppSettings.AstronomicalUnit:F4} AU",
            DistanceUnit.Kilometers => $"{meters / 1000.0:F0} km",
            DistanceUnit.LightYears => $"{meters / AppSettings.LightYear:F5} ly",
            _ => throw new InvalidOperationException("Unknown distance unit.")
        };
    }
}
