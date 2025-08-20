namespace Credo.Core.FileStorage.V1.DB.Models.Upload;

public interface IUploadRouteArgs
{
}

public sealed record AliasArgs(string ChannelAlias, string OperationAlias) : IUploadRouteArgs;

public sealed record ExternalAliasArgs(string ChannelExternalAlias, string OperationExternalAlias) : IUploadRouteArgs;

public sealed record ExternalIdArgs(long ChannelExternalId, long OperationExternalId) : IUploadRouteArgs;

public sealed record ChOpBucketArgs(Guid ChannelOperationBucketId) : IUploadRouteArgs;

public sealed record BucketNameArgs(string BucketName) : IUploadRouteArgs; // seeds/binds default/default