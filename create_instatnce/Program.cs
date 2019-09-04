using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;

namespace create_instatnce
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.Write(GetServiceOutput());
            Console.Read();
            Program Obprogram = new Program();
            Obprogram.CreateaKeyPair();
            Obprogram.create_lunch_checkstatus_for_istance();
            


        }
        // enumerate VPC security group and create a security group for EC2-VPC
        public void create_lunch_checkstatus_for_istance()
        {
            //Create an Amazon EC2 Client Using the the SDK
            var ec2Client = new AmazonEC2Client();
            // enumerate VPC security group 
            string secGroupName = "my-sample-sg-vpc";
            SecurityGroup mySG = null;
            string vpcID = "vpc-7cdc5904";

            Amazon.EC2.Model.Filter vpcFilter = new Amazon.EC2.Model.Filter
            {
                Name = "vpc-id",
                Values = new List<string>() { vpcID }
            };
            var dsgRequest = new DescribeSecurityGroupsRequest();
            dsgRequest.Filters.Add(vpcFilter);
            var dsgResponse = ec2Client.DescribeSecurityGroups(dsgRequest);
            List<SecurityGroup> mySGs = dsgResponse.SecurityGroups;
            foreach (SecurityGroup item in mySGs)
            {
                Console.WriteLine("Existing security group: " + item.GroupId);
                if (item.GroupName == secGroupName)
                {
                    mySG = item;
                }
            }
            //create a security group for EC2-VPC
            if (mySG == null)
            {
                var newSGRequest = new CreateSecurityGroupRequest()
                {
                    GroupName = secGroupName,
                    Description = "My sample security group for EC2-VPC",
                    VpcId = vpcID
                };
                var csgResponse = ec2Client.CreateSecurityGroup(newSGRequest);
                Console.WriteLine();
                Console.WriteLine("New security group: " + csgResponse.GroupId);

                List<string> Groups = new List<string>() { csgResponse.GroupId };
                var newSgRequest = new DescribeSecurityGroupsRequest() { GroupIds = Groups };
                var newSgResponse = ec2Client.DescribeSecurityGroups(newSgRequest);
                mySG = newSgResponse.SecurityGroups[0];
            }
            //Create and initialize an IpPermission object.

            //iprange = the IP addresses of your local machine 
            string ipRange = "0.0.0.0/0";

            List<string> ranges = new List<string>() { ipRange };

            var ipPermission = new IpPermission()
            {
                IpProtocol = "tcp",
                //The beginning and end of the port range. This example specifies a single port, 3389, which is used to communicate with Windows over RDP.
                //it should be changed if u launch a linux instance (use 22 insted )
                FromPort = 3389,
                ToPort = 3389,
                IpRanges = ranges
            };
            //Create and initialize an AuthorizeSecurityGroupIngressRequest object.

            var ingressRequest = new AuthorizeSecurityGroupIngressRequest();
            ingressRequest.GroupId = mySG.GroupId;
            ingressRequest.IpPermissions.Add(ipPermission);
            //Pass the request object to the AuthorizeSecurityGroupIngress method, which returns an AuthorizeSecurityGroupIngressResponse object.
            var ingressResponse = ec2Client.AuthorizeSecurityGroupIngress(ingressRequest);
            Console.WriteLine("New RDP rule for: " + ipRange);

            //Create and initialize a network interface.for lunch enstance 
            string subnetID = "subnet-048d6c59";

            List<string> groups = new List<string>() { mySG.GroupId };
            var eni = new InstanceNetworkInterfaceSpecification()
            {
                DeviceIndex = 0,
                SubnetId = subnetID,
                Groups = groups,
                AssociatePublicIpAddress = true
            };
             List<InstanceNetworkInterfaceSpecification> enis = new List<InstanceNetworkInterfaceSpecification>() { eni };

            string amiID = "ami-06a0d33fc8d328de0";
            string keyPairName = "my-sample-key";

            var launchRequest = new RunInstancesRequest()
            {
                ImageId = amiID,
                InstanceType = "m3.large",
                MinCount = 1,
                MaxCount = 1,
                KeyName = keyPairName,
                NetworkInterfaces = enis
            };

            //launch 
            RunInstancesResponse launchResponse = ec2Client.RunInstances(launchRequest);

            List<String> instanceIds = new List<string>();
            foreach (Instance instance in launchResponse.Reservation.Instances)
            {
                Console.WriteLine(instance.InstanceId);
                instanceIds.Add(instance.InstanceId);
            }
            //check the status of the enstance 
            var instanceRequest = new DescribeInstancesRequest();
            instanceRequest.InstanceIds = new List<string>();
            instanceRequest.InstanceIds.AddRange(instanceIds);
            var response = ec2Client.DescribeInstances(instanceRequest);
            Console.WriteLine(response.Reservations[0].Instances[0].State.Name);

             
        }
        public void CreateaKeyPair()
        {
            //Create an Amazon EC2 Client Using the the SDK
            var ec2Client = new AmazonEC2Client();
            //enumerate the  key pairs
            string keyPairName = "my-sample-key";
            KeyPairInfo myKeyPair = null;

            var dkpRequest = new DescribeKeyPairsRequest();
            var dkpResponse = ec2Client.DescribeKeyPairs(dkpRequest);
            List<KeyPairInfo> myKeyPairs = dkpResponse.KeyPairs;

            foreach (KeyPairInfo item in myKeyPairs)
            {
                Console.WriteLine("Existing key pair: " + item.KeyName);
                if (item.KeyName == keyPairName)
                {
                    myKeyPair = item;
                }
            }
            //create a key pair and save the private key
            if (myKeyPair == null)
            {
                var newKeyRequest = new CreateKeyPairRequest()
                {
                    KeyName = keyPairName
                };
                var ckpResponse = ec2Client.CreateKeyPair(newKeyRequest);
                Console.WriteLine();
                Console.WriteLine("New key: " + keyPairName);

                // Save the private key in a .pem file
                using (FileStream s = new FileStream(keyPairName + ".pem", FileMode.Create))
                using (StreamWriter writer = new StreamWriter(s))
                {
                    writer.WriteLine(ckpResponse.KeyPair.KeyMaterial);
                }
            }
        }
     


        public static string GetServiceOutput()
        {
            StringBuilder sb = new StringBuilder(1024);
            using (StringWriter sr = new StringWriter(sb))
            {
                sr.WriteLine("===========================================");
                sr.WriteLine("Welcome to the AWS .NET SDK!");
                sr.WriteLine("===========================================");

                // Print the number of Amazon EC2 instances.
                IAmazonEC2 ec2 = new AmazonEC2Client();
                DescribeInstancesRequest ec2Request = new DescribeInstancesRequest();

                try
                {
                    DescribeInstancesResponse ec2Response = ec2.DescribeInstances(ec2Request);
                    int numInstances = 0;
                    numInstances = ec2Response.Reservations.Count;
                    sr.WriteLine(string.Format("You have {0} Amazon EC2 instance(s) running in the {1} region.",
                                               numInstances, ConfigurationManager.AppSettings["AWSRegion"]));
                }
                catch (AmazonEC2Exception ex)
                {
                    if (ex.ErrorCode != null && ex.ErrorCode.Equals("AuthFailure"))
                    {
                        sr.WriteLine("The account you are using is not signed up for Amazon EC2.");
                        sr.WriteLine("You can sign up for Amazon EC2 at http://aws.amazon.com/ec2");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Error Type: " + ex.ErrorType);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }
                sr.WriteLine();

                // Print the number of Amazon SimpleDB domains.
                IAmazonSimpleDB sdb = new AmazonSimpleDBClient();
                ListDomainsRequest sdbRequest = new ListDomainsRequest();

                try
                {
                    ListDomainsResponse sdbResponse = sdb.ListDomains(sdbRequest);

                    int numDomains = 0;
                    numDomains = sdbResponse.DomainNames.Count;
                    sr.WriteLine(string.Format("You have {0} Amazon SimpleDB domain(s) in the {1} region.",
                                               numDomains, ConfigurationManager.AppSettings["AWSRegion"]));
                }
                catch (AmazonSimpleDBException ex)
                {
                    if (ex.ErrorCode != null && ex.ErrorCode.Equals("AuthFailure"))
                    {
                        sr.WriteLine("The account you are using is not signed up for Amazon SimpleDB.");
                        sr.WriteLine("You can sign up for Amazon SimpleDB at http://aws.amazon.com/simpledb");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Error Type: " + ex.ErrorType);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }
                sr.WriteLine();

                // Print the number of Amazon S3 Buckets.
                IAmazonS3 s3Client = new AmazonS3Client();

                try
                {
                    ListBucketsResponse response = s3Client.ListBuckets();
                    int numBuckets = 0;
                    if (response.Buckets != null &&
                        response.Buckets.Count > 0)
                    {
                        numBuckets = response.Buckets.Count;
                    }
                    sr.WriteLine("You have " + numBuckets + " Amazon S3 bucket(s).");
                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") ||
                        ex.ErrorCode.Equals("InvalidSecurity")))
                    {
                        sr.WriteLine("Please check the provided AWS Credentials.");
                        sr.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }
                sr.WriteLine("Press any key to continue...");
            }
            return sb.ToString();
        }
    }
}