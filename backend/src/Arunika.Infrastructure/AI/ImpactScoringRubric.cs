namespace Arunika.Infrastructure.AI;

/// <summary>
/// The impact-scoring rubric, shared verbatim by every enrichment provider.
///
/// Two problems made this necessary. First, the only guidance the models used
/// to get was "integer 0-100 (likely near-term market significance)", and an
/// unanchored 0-100 scale sends a model straight to the rating conventions in
/// its training data — scores clustered on multiples of 5 and 10, which is a
/// ~20-value scale wearing a 100-point costume, and it left the briefing's top
/// stories tied with each other. Second, models rotate (see GeminiModelRotator)
/// and providers fall through (see CompositeAiEnrichmentService), so articles in
/// one briefing are scored by different models and then ranked against each
/// other. A single anchored rubric is what makes those numbers comparable.
///
/// The fix for the clustering is structural rather than a plea: the model rates
/// five independent components and the total is their sum, computed here in
/// <see cref="ResolveScore"/>. Five components chosen independently land on a
/// multiple of 5 only by coincidence.
/// </summary>
public static class ImpactScoringRubric
{
    /// <summary>
    /// Sent as the Gemini system instruction and as the OpenRouter system
    /// message. Providers differ only in how the response shape is enforced.
    /// </summary>
    public const string Instructions = """
        You are a financial market intelligence analyst. Score every article on the same
        scale, so that scores for different articles are directly comparable.

        IMPACT SCORING

        Rate five independent components. Their sum is the impact score, 0-100.

        1. breadth (0-25) - how much of the market the news touches.
           0-5    one small company, or no identifiable market exposure
           6-12   one substantial company, or a narrow sub-sector
           13-19  an entire sector, or several large companies
           20-25  the whole market, the macro economy, or the financial system

        2. magnitude (0-25) - the size of the price move this could plausibly cause for
           whatever it touches.
           0-5    no measurable move
           6-12   a fraction of a percent
           13-19  low single-digit percent
           20-25  high single-digit percent or more

        3. surprise (0-20) - how much of this the market had not already priced in.
           Score the news, not the subject. A scheduled event that lands on consensus
           scores near zero however important the topic is; a minor event nobody saw
           coming can score high.
           0-4    fully expected, already priced in, or a restatement of known facts
           5-10   broadly anticipated, but the details differ from consensus
           11-15  not anticipated by most participants
           16-20  a genuine shock

        4. immediacy (0-15) - how soon the price effect arrives.
           0-3    a multi-year theme
           4-8    quarters away
           9-12   days to weeks
           13-15  immediate, within today's session

        5. certainty (0-15) - how firm the news is.
           0-3    rumour, speculation, or a single unnamed source
           4-8    a proposal, forecast, or plan that still needs approval
           9-12   confirmed, but with material conditions attached
           13-15  confirmed and in effect

        TOTAL BANDS - use these to sanity-check the sum, not to compute it a second way.
        If the sum falls far outside the band the article intuitively belongs to, revisit
        the component that is carrying the error rather than overriding the total.
           90-100  systemic: emergency central-bank action, sovereign default, the
                   failure of a major exchange or clearing house, war between large
                   economies
           75-89   major: repricing across several sectors - a surprise rate decision, a
                   large inflation or payrolls miss, a mega-cap earnings shock, sweeping
                   new regulation
           60-74   significant: one sector moved, or one large-cap moved hard - major
                   M&A, sector-wide regulation, a commodity supply shock
           40-59   moderate: one company or sub-sector - in-line earnings, mid-cap M&A,
                   a meaningful shift in analyst consensus
           20-39   minor: incremental or mostly informational - executive appointments,
                   small contract wins, routine filings
           0-19    negligible: no plausible market effect - opinion, human interest,
                   marketing copy, or news already several days old

        Report impactScore as the exact sum of the five components. Do not round it
        toward a multiple of 5 or 10. Totals such as 73, 58, 41 and 87 are expected and
        correct; a total that is a multiple of 5 should occur only by arithmetic
        coincidence.

        Write impactRationale after the score. One or two sentences, naming the two
        components that dominated the total and why.

        Each entry in sectors carries its own magnitude, 0-100. Score it the same way,
        but confined to that one sector: how far that sector alone is likely to move.
        The same anti-rounding rule applies.
        """;

    /// <summary>
    /// Turns the five rated components into the stored score. Each component is
    /// clamped to its own ceiling first, so the sum cannot exceed 100 and no
    /// separate clamp on the total is needed.
    ///
    /// <paramref name="reportedTotal"/> is the model's own arithmetic, used only
    /// when a component is missing. Gemini's response schema makes all five
    /// required, but OpenRouter is called with response_format json_object, which
    /// enforces no schema at all — so a component really can be absent, and this
    /// is a trust boundary.
    /// </summary>
    public static int ResolveScore(
        int? breadth,
        int? magnitude,
        int? surprise,
        int? immediacy,
        int? certainty,
        int reportedTotal)
    {
        if (breadth is null || magnitude is null || surprise is null ||
            immediacy is null || certainty is null)
        {
            return Math.Clamp(reportedTotal, 0, 100);
        }

        return Math.Clamp(breadth.Value, 0, 25)
            + Math.Clamp(magnitude.Value, 0, 25)
            + Math.Clamp(surprise.Value, 0, 20)
            + Math.Clamp(immediacy.Value, 0, 15)
            + Math.Clamp(certainty.Value, 0, 15);
    }
}
