namespace SWLOR.Architecture.Tests.Domains;

/// <summary>Which way a reference between two <see cref="OrderNode"/>s goes in the domain order.</summary>
internal enum OrderDirection
{
    /// <summary>Both ends are the same node.</summary>
    Inside,

    /// <summary>Same band, to a domain lower in the order: allowed.</summary>
    Down,

    /// <summary>Same band, to a domain higher in the order: a violation of the order (UpwardEdges.txt).</summary>
    Up,

    /// <summary>Content using logic of any domain: allowed, content is a band above all logic.</summary>
    ContentUsesLogic,

    /// <summary>Logic naming content of any domain, its own included: the no-content rule (LogicToContent.txt), not the order.</summary>
    LogicNamesContent
}
