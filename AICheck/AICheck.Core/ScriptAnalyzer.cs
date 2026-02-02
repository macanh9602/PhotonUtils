using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace AICheck.Core
{
    public static class ScriptAnalyzer
    {
        public static string ProcessScript(string sourceCode)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            var rewriter = new UnityMemberRewriter();
            var result = rewriter.Visit(root);

            return result?.ToFullString() ?? sourceCode;
        }
    }

    /// <summary>
    /// Rewriter chịu trách nhiệm sắp xếp member trong class theo chuẩn Unity + Region
    /// </summary>
    internal class UnityMemberRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var inspector = new List<MemberDeclarationSyntax>();
            var runtime = new List<MemberDeclarationSyntax>();
            var lifecycle = new List<MemberDeclarationSyntax>();
            var publicApi = new List<MemberDeclarationSyntax>();
            var internalLogic = new List<MemberDeclarationSyntax>();
            var debug = new List<MemberDeclarationSyntax>();

            foreach (var member in node.Members)
            {
                Classify(
                    member,
                    inspector,
                    runtime,
                    lifecycle,
                    publicApi,
                    internalLogic,
                    debug
                );
            }

            // Sort Unity lifecycle theo thứ tự logic (không alphabet)
            lifecycle = SortUnityLifecycle(lifecycle);

            var newMembers = new List<MemberDeclarationSyntax>();

            newMembers.AddRange(AddRegion(inspector, "=== INSPECTOR ==="));
            newMembers.AddRange(AddRegion(runtime, "=== RUNTIME DATA ==="));
            newMembers.AddRange(AddRegion(lifecycle, "=== UNITY LIFECYCLE ==="));
            newMembers.AddRange(AddRegion(publicApi, "=== PUBLIC API ==="));
            newMembers.AddRange(AddRegion(internalLogic, "=== INTERNAL LOGIC ==="));
            newMembers.AddRange(AddRegion(debug, "=== DEBUG ==="));

            return node.WithMembers(SyntaxFactory.List(newMembers));
        }

        #region === CLASSIFICATION ===

        private void Classify(
            MemberDeclarationSyntax member,
            List<MemberDeclarationSyntax> inspector,
            List<MemberDeclarationSyntax> runtime,
            List<MemberDeclarationSyntax> lifecycle,
            List<MemberDeclarationSyntax> publicApi,
            List<MemberDeclarationSyntax> internalLogic,
            List<MemberDeclarationSyntax> debug)
        {
            // FIELD
            if (member is FieldDeclarationSyntax field)
            {
                if (field.AttributeLists.Any())
                    inspector.Add(member);
                else
                    runtime.Add(member);

                return;
            }

            // METHOD
            if (member is MethodDeclarationSyntax method)
            {
                var name = method.Identifier.Text;

                if (IsUnityMessage(name))
                {
                    lifecycle.Add(member);
                    return;
                }

                if (IsDebugMethod(method))
                {
                    debug.Add(member);
                    return;
                }

                if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    publicApi.Add(member);
                    return;
                }

                internalLogic.Add(member);
                return;
            }

            // PROPERTY / EVENT / OTHER
            if (member.Modifiers.Any(SyntaxKind.PublicKeyword))
                publicApi.Add(member);
            else
                internalLogic.Add(member);
        }

        #endregion

        #region === REGION HELPERS ===

        private IEnumerable<MemberDeclarationSyntax> AddRegion(
            List<MemberDeclarationSyntax> members,
            string regionName)
        {
            if (members.Count == 0)
                return Enumerable.Empty<MemberDeclarationSyntax>();

            var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
                .WithEndOfDirectiveToken(
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.PreprocessingMessage($" {regionName}")
                        ),
                        SyntaxKind.EndOfDirectiveToken,
                        SyntaxFactory.TriviaList()
                    )
                );

            var endRegionTrivia = SyntaxFactory.EndRegionDirectiveTrivia(true);

            members[0] = members[0].WithLeadingTrivia(
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Trivia(regionTrivia),
                SyntaxFactory.CarriageReturnLineFeed
            );

            members[^1] = members[^1].WithTrailingTrivia(
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Trivia(endRegionTrivia),
                SyntaxFactory.CarriageReturnLineFeed
            );

            return members;
        }

        #endregion

        #region === UNITY RULES ===

        private static readonly string[] UnityLifecycleOrder =
        {
            "Reset",
            "Awake",
            "OnEnable",
            "OnValidate",
            "Start",
            "Update",
            "LateUpdate",
            "FixedUpdate",
            "OnDisable",
            "OnDestroy",
            "OnDrawGizmos",
            "OnDrawGizmosSelected"
        };

        private bool IsUnityMessage(string name)
            => UnityLifecycleOrder.Contains(name);

        private List<MemberDeclarationSyntax> SortUnityLifecycle(
            List<MemberDeclarationSyntax> members)
        {
            return members
                .OfType<MethodDeclarationSyntax>()
                .OrderBy(m =>
                {
                    var index = UnityLifecycleOrder
                        .Select((n, i) => (n, i))
                        .FirstOrDefault(x => x.n == m.Identifier.Text);

                    return index == default ? int.MaxValue : index.i;
                })
                .Cast<MemberDeclarationSyntax>()
                .ToList();
        }

        private bool IsDebugMethod(MethodDeclarationSyntax method)
        {
            // ContextMenu / Gizmos
            if (method.AttributeLists
                .SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString().Contains("ContextMenu")))
                return true;

            var name = method.Identifier.Text;

            // Naming convention
            if (name.Contains("Debug") || name.Contains("Test"))
                return true;

            // #if DEBUG / #if UNITY_EDITOR
            return method
                .GetLeadingTrivia()
                .Any(t =>
                    t.IsKind(SyntaxKind.IfDirectiveTrivia) &&
                    t.ToString().Contains("DEBUG") ||
                    t.ToString().Contains("UNITY_EDITOR")
                );
        }


        #endregion
    }
}
