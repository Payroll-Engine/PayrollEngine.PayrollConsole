using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Scripting;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Client.Scripting.Script;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.Script;

internal sealed class Publisher
{
    internal Publisher(PayrollHttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    private PayrollHttpClient HttpClient { get; }

    #region Publish

    internal async Task<int> Publish(string sourceFile, string sourceScript = null)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            throw new ArgumentException(nameof(sourceFile));
        }

        // parse source file
        var sourceCode = await File.ReadAllTextAsync(sourceFile);
        var functionClasses = ScriptClassParser.FromSource(sourceCode);
        if (!functionClasses.Any())
        {
            throw new ScriptPublishException($"Missing scripting classes in file {sourceFile}.");
        }

        // argument source script (#line number oder function key)
        if (string.IsNullOrWhiteSpace(sourceScript))
        {
            return await PublishAllFunctions(functionClasses);
        }
        return await PublishFunction(functionClasses, sourceScript);
    }

    /// <summary>Publishes all script functions</summary>
    /// <param name="classes">The script parsed classes</param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<int> PublishAllFunctions(IEnumerable<ScriptClass> classes)
    {
        var publishCount = 0;
        foreach (var @class in classes)
        {
            foreach (var method in @class.Methods)
            {
                publishCount += await PublishFunction(@class.FunctionAttribute, method.Value, method.Key);
            }
        }
        return publishCount;
    }

    /// <summary>Publishes function script by name</summary>
    /// <param name="classes">The script parsed classes</param>
    /// <param name="scriptKey">The script key</param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<int> PublishFunction(IEnumerable<ScriptClass> classes, string scriptKey)
    {
        var publishCount = 0;
        foreach (var @class in classes)
        {
            foreach (var method in @class.Methods)
            {
                if (string.Equals(scriptKey, method.Value.ScriptKey))
                {
                    publishCount += await PublishFunction(@class.FunctionAttribute, method.Value, method.Key);
                }
            }
        }
        return publishCount;
    }

    /// <summary>Publishes function script by line number</summary>
    /// <param name="functionAttribute">The function attribute syntax</param>
    /// <param name="scriptAttribute">The script attribute syntax</param>
    /// <param name="method">The file line number</param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<int> PublishFunction(FunctionAttribute functionAttribute, ScriptAttribute scriptAttribute, MethodDeclarationSyntax method)
    {
        var methodBody = method.Body?.ToString();
        if (string.IsNullOrWhiteSpace(methodBody))
        {
            return 0;
        }

        // ensure same content as imported from JSON
        methodBody = methodBody.TrimStart('{', '\r', '\n');
        methodBody = methodBody.TrimEnd(' ', '}');
        if (string.IsNullOrWhiteSpace(methodBody))
        {
            return 0;
        }

        // publish context
        var context = new PublishContext(HttpClient, scriptAttribute, functionAttribute, methodBody);

        // publisher
        var scriptName = context.ScriptAttribute.GetType().Name;
        var publisher = PublishFactory.CreatePublisher(scriptName);

        // publish
        var result = await publisher.PublishAsync(context) ? 1 : 0;
        return result;
    }

    private sealed class CaseAvailableScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var caseScriptAttribute = context.ScriptAttribute as CaseAvailableScriptAttribute;
            if (context.FunctionAttribute is not CaseAvailableFunctionAttribute caseFunctionAttribute || caseScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation = await context.GetRegulationAsync(context.TenantId, caseFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // case
            var service = new CaseService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var @case = await service.GetAsync<Case>(serviceContext, caseScriptAttribute.CaseName);

            // update case
            if (@case != null && !string.Equals(@case.AvailableExpression, context.MethodBody))
            {
                @case.AvailableExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, @case);
                return true;
            }
            return false;
        }
    }

    private sealed class CaseBuildScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var caseFunctionAttribute = context.FunctionAttribute as CaseBuildFunctionAttribute;
            var caseScriptAttribute = context.ScriptAttribute as CaseBuildScriptAttribute;
            if (caseFunctionAttribute == null || caseScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation = await context.GetRegulationAsync(context.TenantId, caseFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // case
            var service = new CaseService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var @case = await service.GetAsync<Case>(serviceContext, caseScriptAttribute.CaseName);

            // update case
            if (@case != null && !string.Equals(@case.BuildExpression, context.MethodBody))
            {
                @case.BuildExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, @case);
                return true;
            }
            return false;
        }
    }

    private sealed class CaseValidateScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var caseFunctionAttribute = context.FunctionAttribute as CaseValidateFunctionAttribute;
            var caseScriptAttribute = context.ScriptAttribute as CaseValidateScriptAttribute;
            if (caseFunctionAttribute == null || caseScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation = await context.GetRegulationAsync(context.TenantId, caseFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // case
            var service = new CaseService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var @case = await service.GetAsync<Case>(serviceContext, caseScriptAttribute.CaseName);

            // update case
            if (@case != null && !string.Equals(@case.ValidateExpression, context.MethodBody))
            {
                @case.ValidateExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, @case);
                return true;
            }
            return false;
        }
    }

    private sealed class CaseRelationBuildScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var caseRelationFunctionAttribute = context.FunctionAttribute as CaseRelationBuildFunctionAttribute;
            var caseRelationScriptAttribute = context.ScriptAttribute as CaseRelationBuildScriptAttribute;
            if (caseRelationFunctionAttribute == null || caseRelationScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation = await context.GetRegulationAsync(context.TenantId,
                caseRelationFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // case relation
            var service = new CaseRelationService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var caseRelation = await service.GetAsync<CaseRelation>(serviceContext,
                caseRelationScriptAttribute.SourceCaseName,
                caseRelationScriptAttribute.TargetCaseName, caseRelationScriptAttribute.SourceCaseSlot,
                caseRelationScriptAttribute.TargetCaseSlot);

            // update case relation
            if (caseRelation != null && !string.Equals(caseRelation.BuildExpression, context.MethodBody))
            {
                caseRelation.BuildExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, caseRelation);
                return true;
            }

            return false;
        }
    }

    private sealed class CaseRelationValidateScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var caseRelationFunctionAttribute = context.FunctionAttribute as CaseRelationValidateFunctionAttribute;
            var caseRelationScriptAttribute = context.ScriptAttribute as CaseRelationValidateScriptAttribute;
            if (caseRelationFunctionAttribute == null || caseRelationScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation = await context.GetRegulationAsync(context.TenantId,
                caseRelationFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // case relation
            var service = new CaseRelationService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var caseRelation = await service.GetAsync<CaseRelation>(serviceContext,
                caseRelationScriptAttribute.SourceCaseName,
                caseRelationScriptAttribute.TargetCaseName, caseRelationScriptAttribute.SourceCaseSlot,
                caseRelationScriptAttribute.TargetCaseSlot);

            // update case relation
            if (caseRelation != null && !string.Equals(caseRelation.BuildExpression, context.MethodBody))
            {
                caseRelation.ValidateExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, caseRelation);
                return true;
            }

            return false;
        }
    }

    private sealed class CollectorStartScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var collectorFunctionAttribute = context.FunctionAttribute as CollectorStartFunctionAttribute;
            var collectorScriptAttribute = context.ScriptAttribute as CollectorStartScriptAttribute;
            if (collectorFunctionAttribute == null || collectorScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, collectorFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // collector
            var service = new CollectorService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var collector = await service.GetAsync<Collector>(serviceContext, collectorScriptAttribute.CollectorName);

            // update collector
            if (collector != null && !string.Equals(collector.StartExpression, context.MethodBody))
            {
                collector.StartExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, collector);
                return true;
            }

            return false;
        }
    }

    private sealed class CollectorApplyScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var collectorFunctionAttribute = context.FunctionAttribute as CollectorApplyFunctionAttribute;
            var collectorScriptAttribute = context.ScriptAttribute as CollectorApplyScriptAttribute;
            if (collectorFunctionAttribute == null || collectorScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, collectorFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // collector
            var service = new CollectorService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var collector = await service.GetAsync<Collector>(serviceContext, collectorScriptAttribute.CollectorName);

            // update collector
            if (collector != null && !string.Equals(collector.ApplyExpression, context.MethodBody))
            {
                collector.ApplyExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, collector);
                return true;
            }

            return false;
        }
    }

    private sealed class CollectorEndScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var collectorFunctionAttribute = context.FunctionAttribute as CollectorEndFunctionAttribute;
            var collectorScriptAttribute = context.ScriptAttribute as CollectorEndScriptAttribute;
            if (collectorFunctionAttribute == null || collectorScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, collectorFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // collector
            var service = new CollectorService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var collector = await service.GetAsync<Collector>(serviceContext, collectorScriptAttribute.CollectorName);

            // update collector
            if (collector != null && !string.Equals(collector.EndExpression, context.MethodBody))
            {
                collector.EndExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, collector);
                return true;
            }

            return false;
        }
    }

    private sealed class WageTypeValueScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var wageTypeFunctionAttribute = context.FunctionAttribute as WageTypeValueFunctionAttribute;
            var wageTypeScriptAttribute = context.ScriptAttribute as WageTypeValueScriptAttribute;
            if (wageTypeFunctionAttribute == null || wageTypeScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, wageTypeFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // wage type
            var service = new WageTypeService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var wageType = await service.GetAsync<WageType>(serviceContext, wageTypeScriptAttribute.WageTypeNumber);

            // update wage type
            if (wageType != null && !string.Equals(wageType.ValueExpression, context.MethodBody))
            {
                wageType.ValueExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, wageType);
                return true;
            }

            return false;
        }
    }

    private sealed class WageTypeResultScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var wageTypeFunctionAttribute = context.FunctionAttribute as WageTypeResultFunctionAttribute;
            var wageTypeScriptAttribute = context.ScriptAttribute as WageTypeResultScriptAttribute;
            if (wageTypeFunctionAttribute == null || wageTypeScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation = await context.GetRegulationAsync(context.TenantId,
                wageTypeFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // wage type
            var service = new WageTypeService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var wageType = await service.GetAsync<WageType>(serviceContext, wageTypeScriptAttribute.WageTypeNumber);

            // update wage type
            if (wageType != null && !string.Equals(wageType.ResultExpression, context.MethodBody))
            {
                wageType.ResultExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, wageType);
                return true;
            }

            return false;
        }
    }

    private sealed class ReportBuildScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var reportFunctionAttribute = context.FunctionAttribute as ReportBuildFunctionAttribute;
            var reportScriptAttribute = context.ScriptAttribute as ReportBuildScriptAttribute;
            if (reportFunctionAttribute == null || reportScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, reportFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // report
            var service = new ReportService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var report = await service.GetAsync<Report>(serviceContext, reportScriptAttribute.ReportName);

            // update report
            if (report != null && !string.Equals(report.BuildExpression, context.MethodBody))
            {
                report.BuildExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, report);
                return true;
            }

            return false;
        }
    }

    private sealed class ReportStartScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var reportFunctionAttribute = context.FunctionAttribute as ReportStartFunctionAttribute;
            var reportScriptAttribute = context.ScriptAttribute as ReportStartScriptAttribute;
            if (reportFunctionAttribute == null || reportScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, reportFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // report
            var service = new ReportService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var report = await service.GetAsync<Report>(serviceContext, reportScriptAttribute.ReportName);

            // update report
            if (report != null && !string.Equals(report.StartExpression, context.MethodBody))
            {
                report.StartExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, report);
                return true;
            }

            return false;
        }
    }

    private sealed class ReportEndScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var reportFunctionAttribute = context.FunctionAttribute as ReportEndFunctionAttribute;
            var reportScriptAttribute = context.ScriptAttribute as ReportEndScriptAttribute;
            if (reportFunctionAttribute == null || reportScriptAttribute == null)
            {
                return false;
            }

            // regulation
            var regulation =
                await context.GetRegulationAsync(context.TenantId, reportFunctionAttribute.RegulationName);
            if (regulation == null)
            {
                return false;
            }

            // report
            var service = new ReportService(context.HttpClient);
            var serviceContext = new RegulationServiceContext(context.TenantId, regulation.Id);
            var report = await service.GetAsync<Report>(serviceContext, reportScriptAttribute.ReportName);

            // update report
            if (report != null && !string.Equals(report.EndExpression, context.MethodBody))
            {
                report.EndExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, serviceContext, report);
                return true;
            }

            return false;
        }
    }

    private sealed class PayrunStartScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var payrunScriptAttribute = context.ScriptAttribute as PayrunStartScriptAttribute;
            if (payrunScriptAttribute == null)
            {
                return false;
            }

            // payrun
            var service = new PayrunService(context.HttpClient);
            var payrun = await service.GetAsync<Payrun>(context.TenantContext, payrunScriptAttribute.PayrunName);

            // update payrun
            if (payrun != null && !string.Equals(payrun.StartExpression, context.MethodBody))
            {
                payrun.StartExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, context.TenantContext, payrun);
                return true;
            }

            return false;
        }
    }

    private sealed class PayrunEmployeeAvailableScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var payrunScriptAttribute = context.ScriptAttribute as PayrunEmployeeAvailableScriptAttribute;
            if (payrunScriptAttribute == null)
            {
                return false;
            }

            // payrun
            var service = new PayrunService(context.HttpClient);
            var payrun = await service.GetAsync<Payrun>(context.TenantContext, payrunScriptAttribute.PayrunName);

            // update payrun
            if (payrun != null && !string.Equals(payrun.EmployeeAvailableExpression, context.MethodBody))
            {
                payrun.EmployeeAvailableExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, context.TenantContext, payrun);
                return true;
            }

            return false;
        }
    }

    private sealed class PayrunEmployeeStartScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var payrunScriptAttribute = context.ScriptAttribute as PayrunEmployeeStartScriptAttribute;
            if (payrunScriptAttribute == null)
            {
                return false;
            }

            // payrun
            var service = new PayrunService(context.HttpClient);
            var payrun = await service.GetAsync<Payrun>(context.TenantContext, payrunScriptAttribute.PayrunName);

            // update payrun
            if (payrun != null && !string.Equals(payrun.EmployeeStartExpression, context.MethodBody))
            {
                payrun.EmployeeStartExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, context.TenantContext, payrun);
                return true;
            }

            return false;
        }
    }

    private sealed class PayrunWageTypeAvailableScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var payrunScriptAttribute = context.ScriptAttribute as PayrunWageTypeAvailableScriptAttribute;
            if (payrunScriptAttribute == null)
            {
                return false;
            }

            // payrun
            var service = new PayrunService(context.HttpClient);
            var payrun = await service.GetAsync<Payrun>(context.TenantContext, payrunScriptAttribute.PayrunName);

            // update payrun
            if (payrun != null && !string.Equals(payrun.WageTypeAvailableExpression, context.MethodBody))
            {
                payrun.WageTypeAvailableExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, context.TenantContext, payrun);
                return true;
            }

            return false;
        }
    }

    private sealed class PayrunEmployeeEndScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var payrunScriptAttribute = context.ScriptAttribute as PayrunEmployeeEndScriptAttribute;
            if (payrunScriptAttribute == null)
            {
                return false;
            }

            // payrun
            var service = new PayrunService(context.HttpClient);
            var payrun = await service.GetAsync<Payrun>(context.TenantContext, payrunScriptAttribute.PayrunName);

            // update payrun
            if (payrun != null && !string.Equals(payrun.EmployeeEndExpression, context.MethodBody))
            {
                payrun.EmployeeEndExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, context.TenantContext, payrun);
                return true;
            }

            return false;
        }
    }

    private sealed class PayrunEndScriptPublisher : IScriptPublisher
    {
        async Task<bool> IScriptPublisher.PublishAsync(PublishContext context)
        {
            var payrunScriptAttribute = context.ScriptAttribute as PayrunEndScriptAttribute;
            if (payrunScriptAttribute == null)
            {
                return false;
            }

            // payrun
            var service = new PayrunService(context.HttpClient);
            var payrun = await service.GetAsync<Payrun>(context.TenantContext, payrunScriptAttribute.PayrunName);

            // update payrun
            if (payrun != null && !string.Equals(payrun.EndExpression, context.MethodBody))
            {
                payrun.EndExpression = context.MethodBody;
                await UpdateScriptObjectAsync(service, context.TenantContext, payrun);
                return true;
            }

            return false;
        }
    }

    private static async Task UpdateScriptObjectAsync<T, TContext, TQuery>(ICrudService<T, TContext, TQuery> service, TContext context, T obj)
        where T : class
        where TContext : IServiceContext
        where TQuery : Query
    {
        try
        {
            await service.UpdateAsync(context, obj);
        }
        catch (Exception exception)
        {
            throw new ScriptPublishException($"Error updating script object: {exception.GetBaseMessage()}", exception);
        }
    }

    #endregion

    #region Factory

    private static class PublishFactory
    {
        // type registration
        // the dictionary value represents the script parameters required by the function attribute ctor
        private static readonly Dictionary<Type, Type> ScriptAttributes = new()
        {
            // case
            { typeof(CaseAvailableScriptAttribute), typeof(CaseAvailableScriptPublisher) },
            { typeof(CaseBuildScriptAttribute), typeof(CaseBuildScriptPublisher) },
            { typeof(CaseValidateScriptAttribute), typeof(CaseValidateScriptPublisher) },
            // case relation
            { typeof(CaseRelationBuildScriptAttribute), typeof(CaseRelationBuildScriptPublisher) },
            { typeof(CaseRelationValidateScriptAttribute), typeof(CaseRelationValidateScriptPublisher) },
            // collector
            { typeof(CollectorStartScriptAttribute), typeof(CollectorStartScriptPublisher) },
            { typeof(CollectorApplyScriptAttribute), typeof(CollectorApplyScriptPublisher) },
            { typeof(CollectorEndScriptAttribute), typeof(CollectorEndScriptPublisher) },
            // wage type
            { typeof(WageTypeValueScriptAttribute), typeof(WageTypeValueScriptPublisher) },
            { typeof(WageTypeResultScriptAttribute), typeof(WageTypeResultScriptPublisher) },
            // report
            { typeof(ReportBuildScriptAttribute), typeof(ReportBuildScriptPublisher) },
            { typeof(ReportStartScriptAttribute), typeof(ReportStartScriptPublisher) },
            { typeof(ReportEndScriptAttribute), typeof(ReportEndScriptPublisher) },
            // payrun
            { typeof(PayrunStartScriptAttribute), typeof(PayrunStartScriptPublisher) },
            { typeof(PayrunEmployeeAvailableScriptAttribute), typeof(PayrunEmployeeAvailableScriptPublisher) },
            { typeof(PayrunEmployeeStartScriptAttribute), typeof(PayrunEmployeeStartScriptPublisher) },
            { typeof(PayrunWageTypeAvailableScriptAttribute), typeof(PayrunWageTypeAvailableScriptPublisher) },
            { typeof(PayrunEmployeeEndScriptAttribute), typeof(PayrunEmployeeEndScriptPublisher) },
            { typeof(PayrunEndScriptAttribute), typeof(PayrunEndScriptPublisher) }
        };

        internal static IScriptPublisher CreatePublisher(string attributeName)
        {
            // function attribute type
            var type = ScriptAttributes.Keys.FirstOrDefault(x => string.Equals(x.Name, attributeName));
            if (type == null)
            {
                throw new NotSupportedException($"Unsupported script function attribute {attributeName}.");
            }

            // constructor parameters from attribute parameters
            var publisherType = ScriptAttributes[type];

            // create publisher
            var publisher = Activator.CreateInstance(publisherType, null) as IScriptPublisher;
            return publisher;
        }
    }

    #endregion

}