import * as React from 'react';
import * as dotnetify from 'dotnetify';
import ReactPaginate from 'react-paginate'

export default class ChatLogs extends React.Component {
    constructor(props) {
        super(props);
        this.vm = dotnetify.react.connect('ChatLogsViewModel', this);

        this.dispatchState = state => this.vm.$dispatch(state);

        this.state = { PaginatedItems: [], Pages: 1, SelectedPage: 0 }

        this.pageChanged = this.pageChanged.bind(this);
    }

    componentWillUnmount() {

        this.vm.$destroy();
    }

    pageChanged(page) {
        const pageIndex = page.selected;

        this.setState({
            SelectedPage: pageIndex
        });

        this.dispatchState({ ChangePage: pageIndex });
    }

    render() {

        return (
            <div>
                <div className="row">
                    <table className="table table-responsive">
                        <thead>
                            <tr>
                                <th>Sender Account</th>
                                <th>Sender Character</th>
                                <th>Sender CD Key</th>
                                <th>Receiver Account</th>
                                <th>Receiver Character</th>
                                <th>Receiver CD Key</th>
                                <th>Message</th>
                                <th>Date Sent</th>
                            </tr>
                        </thead>
                        <tbody>

                            {this.state.PaginatedItems.map(obj => <tr key={obj.ChatLogID}>
                                <td>
                                    {obj.SenderAccountName}
                                </td>
                                <td>
                                    {obj.SenderPlayer === null
                                        ? obj.SenderDMName
                                        : obj.SenderPlayer.CharacterName}
                                </td>
                                <td>
                                    {obj.SenderCDKey}
                                </td>
                                <td>
                                    {obj.ReceiverAccountName}
                                </td>
                                <td>
                                    {obj.ReceiverPlayer === null
                                        ? obj.ReceiverDMName
                                        : obj.ReceiverPlayer.CharacterName}
                                </td>
                                <td>
                                    {obj.ReceiverCDKey}
                                </td>
                                <td>
                                    {obj.Message}
                                </td>
                                <td>
                                    {obj.DateSent}
                                </td>
                            </tr>)}
                        </tbody>
                    </table>
                </div>

                <div className="row">
                    <ReactPaginate
                        pageCount={this.state.Pages}
                        pageRangeDisplayed={10}
                        marginPagesDisplayed={3}
                        pageClassName="page-item"
                        nextClassName="page-item"
                        previousClassName="page-item"
                        pageLinkClassName="page-link"
                        nextLinkClassName="page-link"
                        previousLinkClassName="page-link"
                        containerClassName="pagination pagination-lg pull-right"
                        onPageChange={this.pageChanged}>
                    </ReactPaginate>

                </div>
            </div>
        );
    }
}
