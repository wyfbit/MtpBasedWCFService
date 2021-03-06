﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.17929
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace AmcMtpService.ServiceReference1 {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ServiceReference1.IDeskTopService")]
    public interface IDeskTopService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/EmergencyDetail", ReplyAction="http://tempuri.org/IDeskTopService/EmergencyDetailResponse")]
        byte[] EmergencyDetail(string Year, int EquipUSE);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/EmergencyDetail", ReplyAction="http://tempuri.org/IDeskTopService/EmergencyDetailResponse")]
        System.Threading.Tasks.Task<byte[]> EmergencyDetailAsync(string Year, int EquipUSE);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetPlanList", ReplyAction="http://tempuri.org/IDeskTopService/GetPlanListResponse")]
        byte[] GetPlanList();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetPlanList", ReplyAction="http://tempuri.org/IDeskTopService/GetPlanListResponse")]
        System.Threading.Tasks.Task<byte[]> GetPlanListAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetEquipMentCheckList", ReplyAction="http://tempuri.org/IDeskTopService/GetEquipMentCheckListResponse")]
        bool GetEquipMentCheckList();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetEquipMentCheckList", ReplyAction="http://tempuri.org/IDeskTopService/GetEquipMentCheckListResponse")]
        System.Threading.Tasks.Task<bool> GetEquipMentCheckListAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/UpLoadFile", ReplyAction="http://tempuri.org/IDeskTopService/UpLoadFileResponse")]
        bool UpLoadFile(System.IO.Stream stream);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/UpLoadFile", ReplyAction="http://tempuri.org/IDeskTopService/UpLoadFileResponse")]
        System.Threading.Tasks.Task<bool> UpLoadFileAsync(System.IO.Stream stream);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetProduct", ReplyAction="http://tempuri.org/IDeskTopService/GetProductResponse")]
        byte[] GetProduct();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetProduct", ReplyAction="http://tempuri.org/IDeskTopService/GetProductResponse")]
        System.Threading.Tasks.Task<byte[]> GetProductAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetTZList", ReplyAction="http://tempuri.org/IDeskTopService/GetTZListResponse")]
        byte[] GetTZList();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetTZList", ReplyAction="http://tempuri.org/IDeskTopService/GetTZListResponse")]
        System.Threading.Tasks.Task<byte[]> GetTZListAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetEmergencyFile", ReplyAction="http://tempuri.org/IDeskTopService/GetEmergencyFileResponse")]
        byte[] GetEmergencyFile();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDeskTopService/GetEmergencyFile", ReplyAction="http://tempuri.org/IDeskTopService/GetEmergencyFileResponse")]
        System.Threading.Tasks.Task<byte[]> GetEmergencyFileAsync();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IDeskTopServiceChannel : AmcMtpService.ServiceReference1.IDeskTopService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class DeskTopServiceClient : System.ServiceModel.ClientBase<AmcMtpService.ServiceReference1.IDeskTopService>, AmcMtpService.ServiceReference1.IDeskTopService {
        
        public DeskTopServiceClient() {
        }
        
        public DeskTopServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public DeskTopServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public DeskTopServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public DeskTopServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public byte[] EmergencyDetail(string Year, int EquipUSE) {
            return base.Channel.EmergencyDetail(Year, EquipUSE);
        }
        
        public System.Threading.Tasks.Task<byte[]> EmergencyDetailAsync(string Year, int EquipUSE) {
            return base.Channel.EmergencyDetailAsync(Year, EquipUSE);
        }
        
        public byte[] GetPlanList() {
            return base.Channel.GetPlanList();
        }
        
        public System.Threading.Tasks.Task<byte[]> GetPlanListAsync() {
            return base.Channel.GetPlanListAsync();
        }
        
        public bool GetEquipMentCheckList() {
            return base.Channel.GetEquipMentCheckList();
        }
        
        public System.Threading.Tasks.Task<bool> GetEquipMentCheckListAsync() {
            return base.Channel.GetEquipMentCheckListAsync();
        }
        
        public bool UpLoadFile(System.IO.Stream stream) {
            return base.Channel.UpLoadFile(stream);
        }
        
        public System.Threading.Tasks.Task<bool> UpLoadFileAsync(System.IO.Stream stream) {
            return base.Channel.UpLoadFileAsync(stream);
        }
        
        public byte[] GetProduct() {
            return base.Channel.GetProduct();
        }
        
        public System.Threading.Tasks.Task<byte[]> GetProductAsync() {
            return base.Channel.GetProductAsync();
        }
        
        public byte[] GetTZList() {
            return base.Channel.GetTZList();
        }
        
        public System.Threading.Tasks.Task<byte[]> GetTZListAsync() {
            return base.Channel.GetTZListAsync();
        }
        
        public byte[] GetEmergencyFile() {
            return base.Channel.GetEmergencyFile();
        }
        
        public System.Threading.Tasks.Task<byte[]> GetEmergencyFileAsync() {
            return base.Channel.GetEmergencyFileAsync();
        }
    }
}
