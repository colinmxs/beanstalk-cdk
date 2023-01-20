using Amazon.CDK;
using Amazon.CDK.AWS.ElasticBeanstalk;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Assets;
using Constructs;

namespace Beanstalk
{
    public class BeanstalkStack : Stack
    {
        internal BeanstalkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // create s3 assets
            var assets = new Asset(this, "Assets", new AssetProps
            {
                Path = "C:\\Users\\smith\\source\\repos\\beanstalk\\src\\App\\bin\\Release\\net6.0\\publish\\app.zip"
            });

            // create elastic beanstalk app
            var appName = "MyApp";
            var app = new CfnApplication(this, "App", new CfnApplicationProps
            {
                ApplicationName = appName
            });

            // create beanstalk application version
            var version = new CfnApplicationVersion(this, "Version", new CfnApplicationVersionProps
            {
                ApplicationName = app.ApplicationName,
                SourceBundle = new CfnApplicationVersion.SourceBundleProperty
                {
                    S3Bucket = assets.S3BucketName,
                    S3Key = assets.S3ObjectKey
                }
            });

            version.AddDependency(app);

            // Create role and instance profile
            var role = new Role(this, "Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com")
            });

            var managedPolicy = ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWebTier");
            role.AddManagedPolicy(managedPolicy);

            var instanceProfileName = $"{appName}-InstanceProfile";
            var instanceProfile = new CfnInstanceProfile(this, "InstanceProfile", new CfnInstanceProfileProps
            {
                InstanceProfileName = instanceProfileName,
                Roles = new[] { role.RoleName }
            });

            // create elastic beanstalk environment
            var optionSettings = new CfnEnvironment.OptionSettingProperty[]
            {
                new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "IamInstanceProfile",
                    Value = instanceProfileName
                },
                new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:elasticbeanstalk:application:environment",
                    OptionName = "ASPNETCORE_ENVIRONMENT",
                    Value = "Production"
                },
                new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:asg",
                    OptionName = "MinSize",
                    Value = "1"
                },
                new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:asg",
                    OptionName = "MaxSize",
                    Value = "1"
                },
                new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:ec2:instances",
                    OptionName = "InstanceTypes",
                    Value = "t2.micro",
                }
            };

            var environment = new CfnEnvironment(this, "Environment", new CfnEnvironmentProps
            {
                ApplicationName = appName,
                EnvironmentName = $"{appName}-Environment",
                OptionSettings = optionSettings,
                SolutionStackName = "64bit Amazon Linux 2 v2.4.3 running .NET Core",
                VersionLabel = version.Ref
            });
        }
    }
}
